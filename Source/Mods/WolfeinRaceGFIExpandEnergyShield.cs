using Multiplayer.API;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandEnergyShieldPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Energy Shield Patch]";

    private const string WolfeinShieldName = "JL_WolfeinExpand.CompWolfeinShield";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        MpCompat.RegisterLambdaMethod(WolfeinShieldName, "CompGetGizmosExtra", 1, 2)
            .SetDebugOnly();

        Register();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void Register()
    {
        var _shieldType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, WolfeinShieldName);
        if (_shieldType == null)
        {
            Log.Error($"{LogPrefix} Could not find {WolfeinShieldName}.");
            return;
        }

        RegisterSyncMethods(_shieldType);
    }

    private static void RegisterSyncMethods(Type _shieldType)
    {
        MP.RegisterSyncMethod(_shieldType, "ToggleShield");
        MP.RegisterSyncMethod(_shieldType, "RechargeShieldWithEnergy");
    }
}