using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Multiplayer Patch for Wolfein Race GFI Expand by 静流的笨蛋喵,
///     Last Update: 2 Aug @ 5:23pm 2026
///     <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=3699095346" />
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

        // Each block is isolated: a bad lookup in one GFI update
        // must not prevent the remaining patches from registering.
        SafePatch(WolfeinRaceGFIExpandChip.Patch);
        SafePatch(WolfeinRaceGFIExpandRandom.Patch);
        SafePatch(WolfeinRaceGFIExpandEnergyShield.Patch);
        SafePatch(WolfeinRaceGFIExpandWingman.Patch);
        SafePatch(WolfeinRaceGFIExpandArtificialMoon.Patch);
        SafePatch(WolfeinRaceGFIExpandHairStyle.Patch);
        SafePatch(WolfeinRaceGFIExpandAbility.Patch);
        SafePatch(WolfeinRaceGFIExpandWeapon.Patch);
        SafePatch(WolfeinRaceGFIExpandHologram.Patch);
        SafePatch(WolfeinRaceGFIExpandTurret.Patch);

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void SafePatch(Action patch)
    {
        try
        {
            patch();
        }
        catch (Exception exception)
        {
            Log.Error($"{LogPrefix} Patch {patch.Method.Name} failed: {exception}");
        }
    }
}