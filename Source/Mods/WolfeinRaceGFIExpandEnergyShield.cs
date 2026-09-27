using Multiplayer.API;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandEnergyShield
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Energy Shield Patch]";

    private const string WolfeinShieldName = "JL_WolfeinExpand.CompWolfeinShield";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var shieldType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, WolfeinShieldName);
        if (shieldType == null)
        {
            Log.Error($"{LogPrefix} Could not find {WolfeinShieldName}.");
            return;
        }

        // DEV gizmos inside CompGetGizmosExtra (debug-only actions).
        // Best effort: ordinals may shift with mod updates; failures are logged
        // by MpCompat and do not break the essential Toggle/Recharge sync below.
        try
        {
            MpCompat.RegisterLambdaMethod(WolfeinShieldName, "CompGetGizmosExtra", 1, 2)
                .SetDebugOnly();
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Could not register dev gizmo lambdas: {exception.Message}");
        }

        RegisterSyncMethods(shieldType);

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void RegisterSyncMethods(Type shieldType)
    {
        MP.RegisterSyncMethod(shieldType, "ToggleShield");
        MP.RegisterSyncMethod(shieldType, "RechargeShieldWithEnergy");
    }
}
