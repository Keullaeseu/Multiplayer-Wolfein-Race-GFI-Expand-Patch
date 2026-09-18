using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandRandomPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Random Patch]";

    private const string HediffCompObservedThoughtGiver =
        "JL_WolfeinExpand.HediffComp_ObservedThoughtGiver:CompPostTick";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchingUtilities.PatchPushPopRand(HediffCompObservedThoughtGiver);

        Log.Message($"{LogPrefix} Initialized.");
    }
}