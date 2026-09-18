using System.Reflection;
using HarmonyLib;
using JL_WolfeinExpand;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using UnityEngine;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public class WolfeinRaceGFIExpandWingmanDroneUIPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Wingman Drone Patch]";

    private static FieldInfo hediffCompField;
    private static MethodInfo getWorkModeNameMethod;
    private static MethodInfo getWorkModeIconPathMethod;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var _wingmanSystemPanelType = AccessTools.TypeByName("JL_WolfeinExpand.Gizmo_WingmanSystemPanel");

        if (_wingmanSystemPanelType == null)
        {
            Log.Warning($"{LogPrefix} Could not find wingman panel type.");
            return;
        }

        var _showWorkModeMenu =
            AccessTools.DeclaredMethod(_wingmanSystemPanelType, "ShowWorkModeMenu");

        if (_showWorkModeMenu == null)
        {
            Log.Warning($"{LogPrefix} Could not find ShowWorkModeMenu.");
            return;
        }

        hediffCompField = AccessTools.Field(_wingmanSystemPanelType, "hediffComp");
        getWorkModeNameMethod = AccessTools.Method(_wingmanSystemPanelType, "GetWorkModeName");
        getWorkModeIconPathMethod = AccessTools.Method(_wingmanSystemPanelType, "GetWorkModeIconPath");

        if (hediffCompField == null || getWorkModeNameMethod == null || getWorkModeIconPathMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find required wingman panel members.");
            return;
        }

        MpCompat.harmony.Patch(_showWorkModeMenu,
            new HarmonyMethod(typeof(WolfeinRaceGFIExpandWingmanDroneUIPatch), nameof(ShowWorkModeMenuPrefix)));

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    public static void RegisterSyncMethods()
    {
        MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandWingmanDroneUIPatch), nameof(SetWorkMode));
    }

    #endregion

    private static bool ShowWorkModeMenuPrefix(object __instance)
    {
        var _wingmanSystem = hediffCompField.GetValue(__instance) as HediffComp_WingmanSystem;

        if (_wingmanSystem == null)
            return false;

        var _pawn = _wingmanSystem.parent?.pawn;

        if (_pawn == null)
            return false;

        DroneWorkMode[] _modes =
        {
            DroneWorkMode.Work,
            DroneWorkMode.Escort,
            DroneWorkMode.Recharge,
            DroneWorkMode.SelfShutdown
        };

        List<FloatMenuOption> _options = new();

        foreach (var _mode in _modes)
        {
            var _capturedMode = _mode;

            var _label = (string)getWorkModeNameMethod.Invoke(__instance, new object[] { _capturedMode });
            var _iconPath = (string)getWorkModeIconPathMethod.Invoke(__instance, new object[] { _capturedMode });
            var _icon = ContentFinder<Texture2D>.Get(_iconPath);

            var _action = () => { SetWorkMode(_pawn, (int)_capturedMode); };

            _options.Add(new FloatMenuOption(_label, _action, _icon, Color.white));
        }

        Find.WindowStack.Add(new FloatMenu(_options));

        return false;
    }

    private static void SetWorkMode(Pawn _pawn, int _modeValue)
    {
        if (_pawn == null || _pawn.health?.hediffSet == null)
            return;

        var _wingmanSystem = FindWingmanSystem(_pawn);
        if (_wingmanSystem == null)
            return;

        var _mode = (DroneWorkMode)_modeValue;
        _wingmanSystem.currentWorkMode = _mode;

        if (_mode == DroneWorkMode.Work)
            TryDeployDronesForWorkMode(_wingmanSystem);
        else if
            (_mode == DroneWorkMode.Escort) TryDeployDronesForEscortMode(_wingmanSystem);

        if (_mode != DroneWorkMode.SelfShutdown)
            foreach (var _drone in _wingmanSystem.GetDeployedDrones())
            {
                if (_drone == null || !_drone.Spawned || _drone.Dead)
                    continue;

                if (_drone is Pawn_PermanentFlyer _permanentFlyer)
                    _permanentFlyer.enablePermanentFlight = true;

                if (_drone.flight != null && !_drone.flight.Flying && _drone.flight.CanFlyNow)
                    _drone.flight.StartFlying();
            }

        foreach (var _drone in _wingmanSystem.GetDeployedDrones())
        {
            if (_drone == null || !_drone.Spawned || _drone.Dead || _drone.jobs == null)
                continue;

            _drone.jobs.StopAll();
            _drone.mindState?.priorityWork.Clear();
            _drone.jobs.CheckForJobOverride();
        }
    }

    private static HediffComp_WingmanSystem FindWingmanSystem(Pawn _pawn)
    {
        if (_pawn?.health?.hediffSet?.hediffs == null)
            return null;

        foreach (var _hediff in _pawn.health.hediffSet.hediffs)
        {
            var _wingmanSystem =
                _hediff.TryGetComp<HediffComp_WingmanSystem>();

            if (_wingmanSystem != null)
                return _wingmanSystem;
        }

        return null;
    }

    #region Try Deploy Drones

    private static void TryDeployDronesForWorkMode(HediffComp_WingmanSystem _wingmanSystem)
    {
        var _storage =
            _wingmanSystem.GetStorage();

        if (_storage == null)
            return;

        var _minimumPower = _wingmanSystem.droneRechargeThresholds.min;

        for (var _slotIndex = 0; _slotIndex < 4; _slotIndex++)
        {
            var _drone = _storage.GetDrone(_slotIndex);

            if (_drone == null || _drone.Spawned)
                continue;

            var _powerCell =
                _drone.TryGetComp<CompMechPowerCell>();

            if (_powerCell == null)
                continue;

            var _power =
                _powerCell.PowerTicksLeft / 2500f;

            if (_power > _minimumPower)
                _storage.DeployDrone(_slotIndex);
        }
    }

    private static void TryDeployDronesForEscortMode(HediffComp_WingmanSystem _wingmanSystem)
    {
        var _storage = _wingmanSystem.GetStorage();

        if (_storage == null)
            return;

        for (var _slotIndex = 0; _slotIndex < 4; _slotIndex++)
        {
            var _drone = _storage.GetDrone(_slotIndex);

            if (_drone == null || _drone.Spawned)
                continue;

            var _powerCell =
                _drone.TryGetComp<CompMechPowerCell>();

            if (_powerCell == null)
                continue;

            var _power =
                _powerCell.PowerTicksLeft / 2500f;

            if (_power > 5.0f)
                _storage.DeployDrone(_slotIndex);
        }
    }

    #endregion
}