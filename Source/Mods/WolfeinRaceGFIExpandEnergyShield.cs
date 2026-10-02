using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Player-facing shield actions (JL_WolfeinExpand.CompWolfeinShield):
///     ToggleShield and RechargeShieldWithEnergy are named methods called
///     from the shield hit-points gizmo, so syncing them directly covers the
///     gizmo. The mod IL additionally shows two capturing void lambdas
///     directly on the comp inside CompGetGizmosExtra (the DEV gizmo
///     actions); those are synced debug-only, same as before.
/// </summary>
public static class WolfeinRaceGFIExpandEnergyShield
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Energy Shield Patch]";

    private const string WolfeinShieldName = "JL_WolfeinExpand.CompWolfeinShield";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var shieldType = WolfeinRaceGFIExpandHelpers.GetTypeByName(LogPrefix, WolfeinShieldName);
        if (shieldType == null)
        {
            Log.Error($"{LogPrefix} Could not find {WolfeinShieldName}.");
            return;
        }

        var synced = 0;
        foreach (var match in WolfeinRaceGFIExpandLambdaSync.FindParentLambdas(shieldType, "CompGetGizmosExtra",
                     typeof(void)))
        {
            MP.RegisterSyncMethod(match).SetDebugOnly();
            synced++;
        }

        if (synced != 2)
            Log.Warning($"{LogPrefix} Expected 2 debug actions, synced {synced}.");
        else
            Log.Message($"{LogPrefix} Synced {WolfeinShieldName}.CompGetGizmosExtra() debug actions.");

        RegisterSyncMethods(shieldType);

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void RegisterSyncMethods(Type shieldType)
    {
        MP.RegisterSyncMethod(shieldType, "ToggleShield");
        MP.RegisterSyncMethod(shieldType, "RechargeShieldWithEnergy");
    }
}