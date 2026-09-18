using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Wolfein Race GFI Expand by 静流的笨蛋喵, Last Update: 2 Aug @ 5:23pm 2026
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3699095346
/// </summary>
[MpCompatFor("JL.WolfeinGFIExpanded")]
public class WolfeinRaceGFIExpandPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Patch]";

    public WolfeinRaceGFIExpandPatch(ModContentPack _content)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        WolfeinRaceGfiExpandChipPatch.Patch();
        WolfeinRaceGFIExpandRandomPatch.Patch();
        WolfeinRaceGFIExpandEnergyShieldPatch.Patch();
        WolfeinRaceGFIExpandWingmanPatch.Patch();
        WolfeinRaceGFIExpandArtificialMoonPatch.Patch();
        WolfeinRaceGFIExpandHairStylePatch.Patch();
        WolfeinRaceGFIExpandAbilityPatch.Patch();

        Log.Message($"{LogPrefix} Initialized.");
    }
}