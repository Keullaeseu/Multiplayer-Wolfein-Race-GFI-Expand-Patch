using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandArtificialMoonPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Artificial Moon Patch]";

    private static Type apparatusType = null!;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        apparatusType = AccessTools.TypeByName("JL_WolfeinExpand.Hediff_MobileArtificialMoonApparatus");

        if (apparatusType == null)
        {
            Log.Error($"{LogPrefix} Could not find Mobile Artificial Moon hediff type.");
            return;
        }

        MP.RegisterSyncMethod(
            typeof(WolfeinRaceGFIExpandArtificialMoonPatch),
            nameof(ToggleMobileMoon),
            new SyncType[] { typeof(Pawn) });

        PatchCommandToggle();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void PatchCommandToggle()
    {
        var _processInput = AccessTools.Method(typeof(Command_Toggle), "ProcessInput");

        if (_processInput == null)
        {
            Log.Error($"{LogPrefix} Could not find Command_Toggle.ProcessInput().");

            return;
        }

        MpCompat.harmony.Patch(
            _processInput,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandArtificialMoonPatch),
                nameof(CommandToggleProcessInputPrefix)));

        Log.Message($"{LogPrefix} Patched Command_Toggle.ProcessInput().");
    }

    private static bool CommandToggleProcessInputPrefix(Command_Toggle __instance)
    {
        if (__instance?.toggleAction == null)
            return true;

        var _target = __instance.toggleAction.Target;

        if (_target == null)
            return true;

        Hediff _apparatus = null;

        if (apparatusType.IsInstanceOfType(_target))
        {
            _apparatus = _target as Hediff;
        }
        else
        {
            var _apparatusField =
                _target.GetType()
                    .GetFields(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public)
                    .FirstOrDefault(_field =>
                        apparatusType.IsAssignableFrom(_field.FieldType));

            if (_apparatusField != null)
                _apparatus = _apparatusField.GetValue(_target) as Hediff;
        }

        if (_apparatus == null)
            return true;

        var _pawn = _apparatus.pawn;

        if (_pawn == null)
        {
            Log.Error($"{LogPrefix} Mobile Artificial Moon has no pawn.");
            return true;
        }

        ToggleMobileMoon(_pawn);

        return false;
    }

    private static void ToggleMobileMoon(Pawn _pawn)
    {
        if (_pawn?.health?.hediffSet == null)
        {
            Log.Error($"{LogPrefix} Pawn has no health or hediff set.");
            return;
        }

        var _apparatus =
            _pawn.health.hediffSet.hediffs.FirstOrDefault(_hediff => apparatusType.IsInstanceOfType(_hediff));

        if (_apparatus == null)
        {
            Log.Error($"{LogPrefix} Could not find Mobile Artificial Moon hediff " + $"on {_pawn}.");
            return;
        }

        var _energyMethod = AccessTools.Method(apparatusType, "HasEnoughMechEnergyToActivate");

        var _switchOnProperty =
            AccessTools.Property(
                apparatusType,
                "SwitchOn");

        if (_switchOnProperty == null)
        {
            Log.Error($"{LogPrefix} Could not find SwitchOn property.");
            return;
        }

        var _switchOn = (bool)_switchOnProperty.GetValue(_apparatus);

        if (!_switchOn)
        {
            var _enoughEnergy =
                _energyMethod != null &&
                (bool)_energyMethod.Invoke(_apparatus, null);

            if (!_enoughEnergy)
            {
                Log.Message(
                    $"{LogPrefix} Mobile Moon cannot activate: insufficient energy.");

                Messages.Message(
                    "JL_PortableMoonlight_LowEnergy".Translate(),
                    _pawn,
                    MessageTypeDefOf.RejectInput);

                return;
            }
        }

        _switchOnProperty.SetValue(_apparatus, !_switchOn);
    }
}