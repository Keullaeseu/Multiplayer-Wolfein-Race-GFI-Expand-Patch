using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Artificial Moon toggle
///     (JL_WolfeinExpand.Hediff_MobileArtificialMoonApparatus).
///     The mod IL shows both lambdas as instance methods directly on the
///     hediff: b__6_0 is the bool isActive getter (UI-only), b__6_1 is the
///     void toggleAction (energy check + switchOn flip). The void
///     return-type filter picks exactly the toggle, so the getter can never
///     be synced by accident. Same pattern as WolfeinArtificialMoon in the
///     base Wolfein patch project (different mod type, no overlap).
/// </summary>
public static class WolfeinRaceGFIExpandArtificialMoon
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Artificial Moon Patch]";

    private const string ApparatusTypeName = "JL_WolfeinExpand.Hediff_MobileArtificialMoonApparatus";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var apparatusType = AccessTools.TypeByName(ApparatusTypeName);

        if (apparatusType == null)
        {
            Log.Error($"{LogPrefix} Could not find Mobile Artificial Moon hediff type.");
            return;
        }

        var synced = WolfeinRaceGFIExpandLambdaSync.SyncParentLambdas(apparatusType, "GetGizmos", typeof(void));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 artificial moon toggle, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {ApparatusTypeName}.GetGizmos().");
    }
}