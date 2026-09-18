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
        PatchRechargeDialog();

        Log.Message($"{LogPrefix} Initialized.");
    }

    public static void RegisterDroneRecharge()
    {
        droneRechargeThresholdsField = MP.RegisterSyncField(wingmanSystemType, "_droneRechargeThresholds");
        MP.RegisterSyncField(wingmanSystemType, "currentWorkMode");
    }

    private static void PatchWingmanSystem()
    {
        wingmanSystemType = TypeByName(WingmanSystemTypeName);
        if (wingmanSystemType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {WingmanSystemTypeName}.");
            return;
        }

        var _rechargeField = Field(wingmanSystemType, "_droneRechargeThresholds");
        if (_rechargeField == null)
            Log.Warning($"{LogPrefix} Could not find " + "HediffComp_WingmanSystem._droneRechargeThresholds.");
    }

    private static void PatchRechargeDialog()
    {
        var _dialogType = TypeByName(RechargeDialogTypeName);

        var _wingmanSystemType = TypeByName(WingmanSystemTypeName);

        if (_dialogType == null || _wingmanSystemType == null)
        {
            Log.Warning($"{LogPrefix} Could not find recharge-dialog types.");
            return;
        }

        dialogWingmanSystemField = FieldRefAccess<object>(_dialogType, "wingmanSystem");

        var _doWindowContents = DeclaredMethod(_dialogType, "DoWindowContents");

        if (_doWindowContents == null)
        {
            Log.Warning($"{LogPrefix} Could not find " + "Dialog_DroneRechargeSettings.DoWindowContents.");
            return;
        }

        MpCompat.harmony.Patch(
            _doWindowContents,
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

        if (dialogWingmanSystemField == null)
            return;

        var _wingmanSystem =
            dialogWingmanSystemField(__instance);

        if (_wingmanSystem == null)
            return;

        MP.WatchBegin();
        droneRechargeThresholdsField.Watch(_wingmanSystem);

        __state = true;
    }

    private static void DroneRechargeDialogPostfix(bool __state)
    {
        if (__state)
            MP.WatchEnd();
    }
}