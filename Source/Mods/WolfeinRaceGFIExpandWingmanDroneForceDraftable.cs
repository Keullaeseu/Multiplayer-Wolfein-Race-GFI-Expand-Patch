using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     CompForceDraftable adds a Draft/Undraft Command_Toggle for drones that lack
///     a vanilla drafter. Vanilla drafting is already synced by MP
///     (Pawn_DraftController), but the custom gizmo lambda that flips
///     Drafted must also be synced, otherwise the toggle only applies locally.
/// </summary>
public static class WolfeinRaceGFIExpandWingmanDroneForceDraftablePatch
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

        // CompGetGizmosExtra yields a single draft toggle; its toggleAction is
        // lambda 0. Best effort - if ordinals shift, MP logs and drafting falls
        // back to vanilla sync (still better than nothing).
        try
        {
            MpCompat.RegisterLambdaMethod(ForceDraftableName, "CompGetGizmosExtra", 0);
            Log.Message($"{LogPrefix} Registered draft toggle lambda.");
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Could not register draft toggle lambda: {exception.Message}");
        }

        Log.Message($"{LogPrefix} Initialized.");
    }

    public static void RegisterSyncMethods()
    {
    }
}