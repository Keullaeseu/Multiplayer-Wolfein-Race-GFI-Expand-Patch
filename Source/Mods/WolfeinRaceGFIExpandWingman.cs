using Multiplayer.API;
using Multiplayer.Compat;
using Verse;
using static HarmonyLib.AccessTools;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandWingmanPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Patch]";

    private const string StorageTypeName = "JL_WolfeinExpand.CompWingmanStorage";

    private const string TurretGunName = "JL_WolfeinExpand.HediffComp_TurretGun";
    private const string WeaponFireModeSwitchName = "JL_WolfeinExpand.CompWeaponFireModeSwitch";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        RegisterStorageActions();
        WolfeinRaceGFIExpandWingmanDroneRechargePatch.Patch();
        WolfeinRaceGFIExpandWingmanDroneRechargePatch.RegisterDroneRecharge();
        WolfeinRaceGFIExpandWingmanDroneUIPatch.Patch();
        WolfeinRaceGFIExpandWingmanDroneUIPatch.RegisterSyncMethods();
        RegisterMethods();

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    private static void RegisterMethods()
    {
        MpCompat.RegisterLambdaMethod(TurretGunName, "CompGetGizmos", 0, 1);

        var _weaponFireModeSwitchType = TypeByName(WeaponFireModeSwitchName);
        if (_weaponFireModeSwitchType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {WeaponFireModeSwitchName}.");
            return;
        }

        MP.RegisterSyncMethod(_weaponFireModeSwitchType, "SwitchFireMode");
    }

    private static void RegisterStorageActions()
    {
        var _storageType =
            TypeByName(StorageTypeName);

        if (_storageType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {StorageTypeName}.");
            return;
        }

        RegisterSyncMethod(_storageType, "DeployAllDrones");
        RegisterSyncMethod(_storageType, "DeployDrone");

        RegisterSyncMethod(_storageType, "TryAddDrone");
        RegisterSyncMethod(_storageType, "TryAddDroneToSlot");

        RegisterSyncMethod(_storageType, "RemoveStoredDroneForTransfer");
        RegisterSyncMethod(_storageType, "RestoreStoredDroneFromTransfer");
    }

    private static void RegisterSyncMethod(Type _declaringType, string _methodName)
    {
        var _method = Method(_declaringType, _methodName);

        if (_method == null)
        {
            Log.Warning($"{LogPrefix} Could not find sync method " + $"{_declaringType.FullName}.{_methodName}.");
            return;
        }

        MP.RegisterSyncMethod(_declaringType, _methodName);
    }

    #endregion
}