using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandAbilityPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Ability Patch]";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        WolfeinRaceGFIExpandAbilityJetJumpPatch.Patch();
        //TODO: WolfeinRaceGFIExpandAbilityAbilityFullSalvoPatch is desync due to explosion sync issue.
        //TODO: Look like a multiplayer mod issue.

        Log.Message($"{LogPrefix} Initialized.");
    }
}