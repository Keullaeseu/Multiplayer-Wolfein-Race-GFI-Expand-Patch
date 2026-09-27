using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandRandom
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Random Patch]";

    private const string HediffCompObservedThoughtGiver =
        "JL_WolfeinExpand.HediffComp_ObservedThoughtGiver:CompPostTick";

    // UI-only particle spawning consumes Verse.Rand during rendering.
    // Must be isolated with Push/Pop so it never perturbs the game RNG.
    private const string WindowParticlesSpawning = "JL_WolfeinExpand.WindowWithParticles:UpdateMouseParticlesSpawning";

    // Bezier projectile lazily initializes its random offset from ExactPosition,
    // which is called from DrawAt (render thread, unsynced) as well as Tick.
    // Isolate it so rendering never perturbs the game RNG. The offset itself
    // may still differ visually per client, but it no longer desyncs gameplay.
    private const string BezierInitRandomOffset = "JL_WolfeinExpand.BezierProjectile:InitRandomOffset";

    // Visual-only lightning arc generation. Isolate in case it is ever
    // triggered from a non-tick context.
    private const string LightningSpawnArc = "JL_WolfeinExpand.LightningArcUtility:SpawnLightningArc";
    private const string LightningSpawnSegment = "JL_WolfeinExpand.LightningArcUtility:SpawnLightningSegment";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchingUtilities.PatchPushPopRand(HediffCompObservedThoughtGiver);
        PatchingUtilities.PatchPushPopRand(WindowParticlesSpawning);
        PatchingUtilities.PatchPushPopRand(BezierInitRandomOffset);
        PatchingUtilities.PatchPushPopRand(LightningSpawnArc);
        PatchingUtilities.PatchPushPopRand(LightningSpawnSegment);

        Log.Message($"{LogPrefix} Initialized.");
    }
}