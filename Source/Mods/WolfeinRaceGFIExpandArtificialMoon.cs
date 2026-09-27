using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Multiplayer.Compat;
using RimWorld;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandArtificialMoon
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Artificial Moon Patch]";

    private const string ApparatusTypeName = "JL_WolfeinExpand.Hediff_MobileArtificialMoonApparatus";

    private static Type apparatusType = null!;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        apparatusType = AccessTools.TypeByName(ApparatusTypeName);

        if (apparatusType == null)
        {
            Log.Error($"{LogPrefix} Could not find Mobile Artificial Moon hediff type.");
            return;
        }

        // Preferred: sync the gizmo toggle lambda directly (standard MpCompat approach).
        // The toggle lambda captures the hediff, so syncing it propagates the
        // switch flip. Best-effort: if ordinal resolution fails we still have
        // the Command_Toggle prefix fallback below.
        TryRegisterToggleLambda();

        MP.RegisterSyncMethod(
            typeof(WolfeinRaceGFIExpandArtificialMoon),
            nameof(SyncedSetSwitch),
            new SyncType[] { typeof(Pawn), typeof(bool) });

        PatchCommandToggle();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void TryRegisterToggleLambda()
    {
        // GetGizmos yields base gizmos then a Command_Toggle with:
        //   isActive = () => switchOn
        //   toggleAction = () => { energy check; switchOn = !switchOn; }
        // toggleAction is expected to be one of the first lambdas. Registering
        // the wrong ordinal (e.g. isActive, evaluated every frame) would spam
        // sync, so only attempt the most likely toggle ordinals and log.
        // If this fails, the prefix below still handles syncing.
        foreach (var lambdaOrdinal in new[] { 1, 0, 2 })
        {
            try
            {
                MpCompat.RegisterLambdaMethod(ApparatusTypeName, "GetGizmos", lambdaOrdinal);
                Log.Message($"{LogPrefix} Registered GetGizmos lambda {lambdaOrdinal} as sync.");
                return;
            }
            catch (Exception exception)
            {
                Log.Message($"{LogPrefix} GetGizmos lambda {lambdaOrdinal} not found: {exception.Message}");
            }
        }

        Log.Warning($"{LogPrefix} Could not register toggle lambda, using ProcessInput prefix fallback.");
    }

    private static void PatchCommandToggle()
    {
        var processInput = AccessTools.Method(typeof(Command_Toggle), "ProcessInput");

        if (processInput == null)
        {
            Log.Error($"{LogPrefix} Could not find Command_Toggle.ProcessInput().");
            return;
        }

        MpCompat.harmony.Patch(
            processInput,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandArtificialMoon),
                nameof(CommandToggleProcessInputPrefix)));

        Log.Message($"{LogPrefix} Patched Command_Toggle.ProcessInput().");
    }

    private static bool CommandToggleProcessInputPrefix(Command_Toggle __instance)
    {
        if (!MP.IsInMultiplayer)
            return true;

        // When the synced method re-executes on remote clients it must not
        // re-enter here. ProcessInput itself is UI-only, but guard anyway.
        if (MP.IsExecutingSyncCommand)
            return true;

        if (__instance?.toggleAction == null)
            return true;

        var toggleTarget = __instance.toggleAction.Target;

        if (toggleTarget == null)
            return true;

        Hediff apparatus = null;

        if (apparatusType.IsInstanceOfType(toggleTarget))
        {
            apparatus = toggleTarget as Hediff;
        }
        else
        {
            var apparatusField =
                toggleTarget.GetType()
                    .GetFields(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public)
                    .FirstOrDefault(field =>
                        apparatusType.IsAssignableFrom(field.FieldType));

            if (apparatusField != null)
                apparatus = apparatusField.GetValue(toggleTarget) as Hediff;
        }

        if (apparatus == null)
            return true;

        var pawn = apparatus.pawn;

        if (pawn == null)
            return true;

        // Read current state on the clicking client and send the explicit
        // target state (idempotent) instead of a blind toggle, so two
        // simultaneous clicks converge instead of double-flipping.
        var switchProperty = AccessTools.Property(apparatusType, "SwitchOn");
        if (switchProperty == null)
            return true;

        var currentSwitchState = (bool)switchProperty.GetValue(apparatus);

        // If lambda sync above succeeded, the original toggleAction is already
        // synced and we would double-apply. Detect that: if the toggleAction's
        // declaring method is registered as sync, let it run instead.
        // We cannot cheaply check registration, so prefer our synced path only
        // when the lambda contains the energy-gated flip (heuristic: target
        // holds an apparatus reference). Always cancel original here to avoid
        // unsynced field flip; the synced method re-applies deterministically.
        SyncedSetSwitch(pawn, !currentSwitchState);

        return false;
    }

    private static void SyncedSetSwitch(Pawn pawn, bool targetState)
    {
        if (pawn?.health?.hediffSet == null)
            return;

        var apparatus =
            pawn.health.hediffSet.hediffs.FirstOrDefault(hediff => apparatusType.IsInstanceOfType(hediff));

        if (apparatus == null)
            return;

        var energyMethod = AccessTools.Method(apparatusType, "HasEnoughMechEnergyToActivate");
        var switchOnProperty = AccessTools.Property(apparatusType, "SwitchOn");

        if (switchOnProperty == null)
            return;

        var currentSwitchState = (bool)switchOnProperty.GetValue(apparatus);

        // Already in target state (e.g. duplicate delivery) - nothing to do.
        if (currentSwitchState == targetState)
            return;

        if (targetState)
        {
            var enoughEnergy =
                energyMethod != null &&
                (bool)energyMethod.Invoke(apparatus, null);

            if (!enoughEnergy)
            {
                // Only the initiator should see the reject message.
                if (!MP.IsExecutingSyncCommand || MP.IsExecutingSyncCommandIssuedBySelf)
                {
                    Messages.Message(
                        "JL_PortableMoonlight_LowEnergy".Translate(),
                        pawn,
                        MessageTypeDefOf.RejectInput);
                }

                return;
            }
        }

        switchOnProperty.SetValue(apparatus, targetState);
    }
}
