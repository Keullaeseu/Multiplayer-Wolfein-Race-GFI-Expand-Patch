using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Wolfein Race GFI Expand by 静流的笨蛋喵, Last Update: 2 Aug @ 5:23pm 2026
///     https://steamcommunity.com/sharedfiles/filedetails/?id=3699095346
/// </summary>
[MpCompatFor("JL.WolfeinGFIExpanded")]
public class WolfeinRaceGFIExpand
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Patch]";

    public WolfeinRaceGFIExpand(ModContentPack content)
    {
        LongEventHandler.ExecuteWhenFinished(LatePatch);
    }

    private static void LatePatch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        WolfeinRaceGfiExpandChip.Patch();
        WolfeinRaceGFIExpandRandom.Patch();
        WolfeinRaceGFIExpandEnergyShield.Patch();
        WolfeinRaceGFIExpandWingman.Patch();
        WolfeinRaceGFIExpandArtificialMoon.Patch();
        WolfeinRaceGFIExpandHairStyle.Patch();
        WolfeinRaceGFIExpandAbility.Patch();
        WolfeinRaceGFIExpandWeapon.Patch();
        WolfeinRaceGFIExpandHologram.Patch();
        WolfeinRaceGFIExpandTurret.Patch();

        Log.Message($"{LogPrefix} Initialized.");
    }
}