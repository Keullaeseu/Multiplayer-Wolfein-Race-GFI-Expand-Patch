using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandAbility
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Ability Patch]";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        WolfeinRaceGFIExpandAbilityJetJump.Patch();
        // FullSalvo (Verb_CastAbility_FullSalvoBurst + BezierProjectile):
        // target-cell calculation itself is deterministic, but BezierProjectile
        // lazily rolls Verse.Rand offsets from DrawAt (render, unsynced).
        // The RNG isolation for that lives in WolfeinRaceGFIExpandRandom
        // (BezierProjectile:InitRandomOffset with Push/Pop) so rendering can no
        // longer corrupt game RNG. Explosion damage itself uses vanilla
        // explosion logic already handled by MP.

        Log.Message($"{LogPrefix} Initialized.");
    }
}