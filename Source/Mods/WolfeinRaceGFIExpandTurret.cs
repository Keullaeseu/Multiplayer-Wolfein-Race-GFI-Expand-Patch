using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Turret gizmos (Command_TurretControl on
///     JL_WolfeinExpand.HediffComp_TurretGun).
///     The mod IL shows both actions as instance methods directly on the
///     comp: b__45_0 flips fireAtWill + clears forcedTarget, b__45_1 takes
///     the forced LocalTargetInfo. Syncing by signature (same pattern as
///     WolfeinRepairUnit in the base Wolfein patch project) instead of
///     ordinals. Derived types (TurretGun2/3/4) inherit CompGetGizmos
///     without overriding it, so the base covers all of them.
/// </summary>
public static class WolfeinRaceGFIExpandTurret
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Turret Patch]";

    private const string BaseTurretTypeName = "JL_WolfeinExpand.HediffComp_TurretGun";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var baseTurretType = AccessTools.TypeByName(BaseTurretTypeName);
        if (baseTurretType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {BaseTurretTypeName}.");
            return;
        }

        var synced = WolfeinRaceGFIExpandLambdaSync.SyncParentLambdas(baseTurretType, "CompGetGizmos", typeof(void));
        synced += WolfeinRaceGFIExpandLambdaSync.SyncParentLambdas(baseTurretType, "CompGetGizmos", typeof(void),
            typeof(LocalTargetInfo));

        if (synced != 2)
        {
            Log.Warning(
                $"{LogPrefix} Expected 2 turret actions, synced {synced}.");
            return;
        }

        Log.Message($"{LogPrefix} Patched {BaseTurretTypeName}.CompGetGizmos().");
    }
}