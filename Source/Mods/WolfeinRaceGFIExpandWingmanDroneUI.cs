using System.Reflection;
using HarmonyLib;
using JL_WolfeinExpand;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using UnityEngine;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public class WolfeinRaceGFIExpandWingmanDroneUI
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Drone Patch]";

    private static FieldInfo hediffCompField;
    private static MethodInfo getWorkModeNameMethod;
    private static MethodInfo getWorkModeIconPathMethod;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var wingmanSystemPanelType = AccessTools.TypeByName("JL_WolfeinExpand.Gizmo_WingmanSystemPanel");

        if (wingmanSystemPanelType == null)
        {
            Log.Warning($"{LogPrefix} Could not find wingman panel type.");
            return;
        }

        var showWorkModeMenu =
            AccessTools.DeclaredMethod(wingmanSystemPanelType, "ShowWorkModeMenu");

        if (showWorkModeMenu == null)
        {
            Log.Warning($"{LogPrefix} Could not find ShowWorkModeMenu.");
            return;
        }

        hediffCompField = AccessTools.Field(wingmanSystemPanelType, "hediffComp");
        getWorkModeNameMethod = AccessTools.Method(wingmanSystemPanelType, "GetWorkModeName");
        getWorkModeIconPathMethod = AccessTools.Method(wingmanSystemPanelType, "GetWorkModeIconPath");

        if (hediffCompField == null || getWorkModeNameMethod == null || getWorkModeIconPathMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find required wingman panel members.");
            return;
        }

        MpCompat.harmony.Patch(showWorkModeMenu,
            new HarmonyMethod(typeof(WolfeinRaceGFIExpandWingmanDroneUI), nameof(ShowWorkModeMenuPrefix)));

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    public static void RegisterSyncMethods()
    {
        MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandWingmanDroneUI), nameof(SetWorkMode));
    }

    #endregion

    private static bool ShowWorkModeMenuPrefix(object __instance)
    {
        // Singleplayer (or when already executing the synced command):
        // let the original menu run so behaviour never diverges from vanilla.
        if (!MP.IsInMultiplayer || MP.IsExecutingSyncCommand)
            return true;

        var wingmanSystem = hediffCompField.GetValue(__instance) as HediffComp_WingmanSystem;

        if (wingmanSystem == null)
            return false;

        var pawn = wingmanSystem.parent?.pawn;

        if (pawn == null)
            return false;

        DroneWorkMode[] workModes =
        {
            DroneWorkMode.Work,
            DroneWorkMode.Escort,
            DroneWorkMode.Recharge,
            DroneWorkMode.SelfShutdown
        };

        List<FloatMenuOption> menuOptions = new();

        foreach (var workMode in workModes)
        {
            var capturedWorkMode = workMode;

            var modeLabel = (string)getWorkModeNameMethod.Invoke(__instance, new object[] { capturedWorkMode });
            var modeIconPath = (string)getWorkModeIconPathMethod.Invoke(__instance, new object[] { capturedWorkMode });
            var modeIcon = ContentFinder<Texture2D>.Get(modeIconPath);

            var menuAction = () => { SetWorkMode(pawn, (int)capturedWorkMode); };

            menuOptions.Add(new FloatMenuOption(modeLabel, menuAction, modeIcon, Color.white));
        }

        Find.WindowStack.Add(new FloatMenu(menuOptions));

        return false;
    }

    private static void SetWorkMode(Pawn pawn, int workModeValue)
    {
        if (pawn == null || pawn.health?.hediffSet == null)
            return;

        var wingmanSystem = FindWingmanSystem(pawn);
        if (wingmanSystem == null)
            return;

        var workMode = (DroneWorkMode)workModeValue;
        wingmanSystem.currentWorkMode = workMode;

        if (workMode == DroneWorkMode.Work)
            TryDeployDronesForWorkMode(wingmanSystem);
        else if
            (workMode == DroneWorkMode.Escort) TryDeployDronesForEscortMode(wingmanSystem);

        if (workMode != DroneWorkMode.SelfShutdown)
            foreach (var drone in wingmanSystem.GetDeployedDrones())
            {
                if (drone == null || !drone.Spawned || drone.Dead)
                    continue;

                if (drone is Pawn_PermanentFlyer permanentFlyer)
                    permanentFlyer.enablePermanentFlight = true;

                if (drone.flight != null && !drone.flight.Flying && drone.flight.CanFlyNow)
                    drone.flight.StartFlying();
            }

        foreach (var drone in wingmanSystem.GetDeployedDrones())
        {
            if (drone == null || !drone.Spawned || drone.Dead || drone.jobs == null)
                continue;

            drone.jobs.StopAll();
            drone.mindState?.priorityWork.Clear();
            drone.jobs.CheckForJobOverride();
        }
    }

    private static HediffComp_WingmanSystem FindWingmanSystem(Pawn pawn)
    {
        if (pawn?.health?.hediffSet?.hediffs == null)
            return null;

        foreach (var hediff in pawn.health.hediffSet.hediffs)
        {
            var wingmanSystem =
                hediff.TryGetComp<HediffComp_WingmanSystem>();

            if (wingmanSystem != null)
                return wingmanSystem;
        }

        return null;
    }

    #region Try Deploy Drones

    private static void TryDeployDronesForWorkMode(HediffComp_WingmanSystem wingmanSystem)
    {
        var storage =
            wingmanSystem.GetStorage();

        if (storage == null)
            return;

        var minimumPower = wingmanSystem.droneRechargeThresholds.min;

        for (var slotIndex = 0; slotIndex < 4; slotIndex++)
        {
            var drone = storage.GetDrone(slotIndex);

            if (drone == null || drone.Spawned)
                continue;

            var powerCell =
                drone.TryGetComp<CompMechPowerCell>();

            if (powerCell == null)
                continue;

            var powerHours =
                powerCell.PowerTicksLeft / 2500f;

            if (powerHours > minimumPower)
                storage.DeployDrone(slotIndex);
        }
    }

    private static void TryDeployDronesForEscortMode(HediffComp_WingmanSystem wingmanSystem)
    {
        var storage = wingmanSystem.GetStorage();

        if (storage == null)
            return;

        for (var slotIndex = 0; slotIndex < 4; slotIndex++)
        {
            var drone = storage.GetDrone(slotIndex);

            if (drone == null || drone.Spawned)
                continue;

            var powerCell =
                drone.TryGetComp<CompMechPowerCell>();

            if (powerCell == null)
                continue;

            var powerHours =
                powerCell.PowerTicksLeft / 2500f;

            if (powerHours > 5.0f)
                storage.DeployDrone(slotIndex);
        }
    }

    #endregion
}