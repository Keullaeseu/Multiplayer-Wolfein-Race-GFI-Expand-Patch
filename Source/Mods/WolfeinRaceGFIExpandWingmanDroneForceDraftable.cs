using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     CompForceDraftable adds a Draft/Undraft Command_Toggle for drones that lack
///     a vanilla drafter. The mod IL shows both lambdas as instance methods
///     directly on the comp: b__6_0 is the bool isActive getter (UI-only),
///     b__6_1 is the void toggleAction (flips Drafted + plays the sound).
///     The void return-type filter picks exactly the toggle, so the getter
///     can never be synced by accident (it runs every frame).
/// </summary>
public static class WolfeinRaceGFIExpandWingmanDroneForceDraftable
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Drone Force Draftable Patch]";

    private const string ForceDraftableName = "JL_WolfeinExpand.CompForceDraftable";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var forceDraftableType = AccessTools.TypeByName(ForceDraftableName);
        if (forceDraftableType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {ForceDraftableName}, skipping.");
            return;
        }

        var synced =
            WolfeinRaceGFIExpandLambdaSync.SyncParentLambdas(forceDraftableType, "CompGetGizmosExtra", typeof(void));

        if (synced != 1)
        {
            Log.Warning(
                $"{LogPrefix} Expected 1 draft toggle, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {ForceDraftableName}.CompGetGizmosExtra().");
    }
}