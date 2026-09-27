using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Turret gizmos (Command_TurretControl):
///     - toggleAction flips fireAtWill + clears forcedTarget
///     - onTargetSelected sets forcedTarget/currentTarget
///     Both are lambdas inside HediffComp_TurretGun.CompGetGizmos.
///     Derived types (TurretGun2/3/4) inherit this method without overriding it,
///     so registering the base covers all of them.
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

        // Ordinals 0,1 = toggleAction + onTargetSelected in CompGetGizmos.
        MpCompat.RegisterLambdaMethod(baseTurretType, "CompGetGizmos", 0, 1);
        Log.Message($"{LogPrefix} Registered {BaseTurretTypeName}.CompGetGizmos lambdas 0,1.");

        Log.Message($"{LogPrefix} Initialized.");
    }
}