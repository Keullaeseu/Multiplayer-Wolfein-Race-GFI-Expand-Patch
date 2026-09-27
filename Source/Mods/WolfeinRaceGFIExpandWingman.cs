using Multiplayer.API;
using Verse;
using static HarmonyLib.AccessTools;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandWingman
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Patch]";

    private const string StorageTypeName = "JL_WolfeinExpand.CompWingmanStorage";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        RegisterStorageActions();
        WolfeinRaceGFIExpandWingmanDroneRechargePatch.Patch();
        WolfeinRaceGFIExpandWingmanDroneUIPatch.Patch();
        WolfeinRaceGFIExpandWingmanDroneUIPatch.RegisterSyncMethods();
        WolfeinRaceGFIExpandWingmanDroneForceDraftablePatch.Patch();

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    private static void RegisterStorageActions()
    {
        var storageType = TypeByName(StorageTypeName);

        if (storageType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {StorageTypeName}.");
            return;
        }

        RegisterSyncMethod(storageType, "DeployAllDrones");
        RegisterSyncMethod(storageType, "DeployDrone");

        RegisterSyncMethod(storageType, "TryAddDrone");
        RegisterSyncMethod(storageType, "TryAddDroneToSlot");

        RegisterSyncMethod(storageType, "RemoveStoredDroneForTransfer");
        RegisterSyncMethod(storageType, "RestoreStoredDroneFromTransfer");
    }

    private static void RegisterSyncMethod(Type declaringType, string methodName)
    {
        var targetMethod = Method(declaringType, methodName);

        if (targetMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find sync method " + $"{declaringType.FullName}.{methodName}.");
            return;
        }

        MP.RegisterSyncMethod(declaringType, methodName);
    }

    #endregion
}