using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     CompExtraTex (JL_WolfeinExpand.CompExtraTex) holds persistent state
///     (selectedPawn, hologram offsets/scale) saved via PostExposeData, so UI
///     edits must be synced.
///     The mod IL shows the gizmo actions as instance methods directly on the
///     comp: b__25_0 opens pawn targeting (UI-only), b__25_1 opens the
///     hologram dialog (UI-only), b__25_3 takes the chosen LocalTargetInfo
///     and assigns selectedPawn. Only the target callback is synced, picked
///     by its void(LocalTargetInfo) signature so the UI-only actions and the
///     bool validator on the shared display class can never match.
///     Slider drags and Reset call the named ApplyHologramTransform method,
///     which is synced directly.
/// </summary>
public static class WolfeinRaceGFIExpandHologram
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Hologram Patch]";

    private const string ExtraTexTypeName = "JL_WolfeinExpand.CompExtraTex";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var extraTexType = AccessTools.TypeByName(ExtraTexTypeName);
        if (extraTexType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {ExtraTexTypeName}.");
            return;
        }

        // Slider drags + Reset call this every change; syncing the method keeps
        // all clients converged. Float args are natively syncable.
        var applyMethod = AccessTools.Method(extraTexType, "ApplyHologramTransform",
            new[] { typeof(float), typeof(float), typeof(float) });
        if (applyMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find {ExtraTexTypeName}.ApplyHologramTransform.");
        }
        else
        {
            MP.RegisterSyncMethod(extraTexType, "ApplyHologramTransform");
            Log.Message($"{LogPrefix} Registered {ExtraTexTypeName}.ApplyHologramTransform.");
        }

        var synced = WolfeinRaceGFIExpandLambdaSync.SyncParentLambdas(extraTexType, "CompGetGizmosExtra", typeof(void),
            typeof(LocalTargetInfo));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 pawn-select callback, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {ExtraTexTypeName}.CompGetGizmosExtra().");
    }
}