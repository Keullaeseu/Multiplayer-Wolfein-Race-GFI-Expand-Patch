using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandWingmanDroneForceDraftablePatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Drone Force Draftable Patch]";

    private const string ForceDraftableName = "JL_WolfeinExpand.CompForceDraftable";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        //  TODO: Patch drone state while in Wingman system. 
        //  TODO: Not will be synced in near future due to complicated logic in original code.

        Log.Message($"{LogPrefix} Initialized.");
    }

    public static void RegisterSyncMethods()
    {
    }
}