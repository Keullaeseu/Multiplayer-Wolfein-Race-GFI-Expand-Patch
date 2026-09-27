using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using Verse;
using static HarmonyLib.AccessTools;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandWingmanDroneRechargePatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Drone Recharge Patch]";

    private const string RechargeDialogTypeName = "JL_WolfeinExpand.Dialog_DroneRechargeSettings";
    private const string WingmanSystemTypeName = "JL_WolfeinExpand.HediffComp_WingmanSystem";

    private static Type wingmanSystemType;
    private static ISyncField droneRechargeThresholdsField;
    private static FieldRef<object, object> dialogWingmanSystemField;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchWingmanSystem();
        RegisterSyncFields();
        PatchRechargeDialog();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void RegisterSyncFields()
    {
        if (wingmanSystemType == null)
            return;

        var rechargeField = Field(wingmanSystemType, "_droneRechargeThresholds");
        if (rechargeField == null)
        {
            Log.Warning($"{LogPrefix} Could not find HediffComp_WingmanSystem._droneRechargeThresholds.");
        }
        else
        {
            droneRechargeThresholdsField = MP.RegisterSyncField(wingmanSystemType, "_droneRechargeThresholds");
            Log.Message($"{LogPrefix} Registered _droneRechargeThresholds sync field.");
        }

        // currentWorkMode is set via synced SetWorkMode (drone UI patch),
        // but also register it so direct sets converge if the mod ever
        // assigns it elsewhere.
        var workModeField = Field(wingmanSystemType, "currentWorkMode");
        if (workModeField == null)
        {
            Log.Warning($"{LogPrefix} Could not find HediffComp_WingmanSystem.currentWorkMode.");
        }
        else
        {
            MP.RegisterSyncField(wingmanSystemType, "currentWorkMode");
            Log.Message($"{LogPrefix} Registered currentWorkMode sync field.");
        }
    }

    private static void PatchWingmanSystem()
    {
        wingmanSystemType = TypeByName(WingmanSystemTypeName);
        if (wingmanSystemType == null) Log.Warning($"{LogPrefix} Could not find {WingmanSystemTypeName}.");
    }

    private static void PatchRechargeDialog()
    {
        var dialogType = TypeByName(RechargeDialogTypeName);

        var resolvedWingmanSystemType = TypeByName(WingmanSystemTypeName);

        if (dialogType == null || resolvedWingmanSystemType == null)
        {
            Log.Warning($"{LogPrefix} Could not find recharge-dialog types.");
            return;
        }

        if (droneRechargeThresholdsField == null)
        {
            Log.Warning($"{LogPrefix} Sync field missing, skipping dialog Watch patch.");
            return;
        }

        try
        {
            dialogWingmanSystemField = FieldRefAccess<object>(dialogType, "wingmanSystem");
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Could not bind Dialog_DroneRechargeSettings.wingmanSystem: {exception.Message}");
            return;
        }

        var doWindowContents = DeclaredMethod(dialogType, "DoWindowContents");

        if (doWindowContents == null)
        {
            Log.Warning($"{LogPrefix} Could not find Dialog_DroneRechargeSettings.DoWindowContents.");
            return;
        }

        MpCompat.harmony.Patch(
            doWindowContents,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandWingmanDroneRechargePatch),
                nameof(DroneRechargeDialogPrefix)),
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandWingmanDroneRechargePatch),
                nameof(DroneRechargeDialogPostfix)));
    }

    private static void DroneRechargeDialogPrefix(object __instance, ref bool __state)
    {
        __state = false;

        if (!MP.IsInMultiplayer)
            return;

        if (MP.IsExecutingSyncCommand)
            return;

        if (dialogWingmanSystemField == null || droneRechargeThresholdsField == null)
            return;

        object wingmanSystem;
        try
        {
            wingmanSystem = dialogWingmanSystemField(__instance);
        }
        catch
        {
            return;
        }

        if (wingmanSystem == null)
            return;

        MP.WatchBegin();
        droneRechargeThresholdsField.Watch(wingmanSystem);

        __state = true;
    }

    private static void DroneRechargeDialogPostfix(bool __state)
    {
        if (__state)
            MP.WatchEnd();
    }
}