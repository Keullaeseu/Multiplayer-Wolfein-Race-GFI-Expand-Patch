using System.Reflection;
using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Verb-lifetime repairs for CompWeaponFireModeSwitch.SwitchFireMode,
///     which builds a fresh secondary verb and swaps it straight into the
///     tracker list (verified in IL).
///     Caster: vanilla never scribes Verb.caster; Pawn_EquipmentTracker.
///     ExposeData repairs it (verb.caster = pawn) on PostLoadInit. GFI copies
///     PrimaryVerb.caster at switch time instead, which is still null when
///     the switch is re-applied during load before that repair runs. A null
///     caster crashes the next gizmo draw inside
///     VerbTracker.CreateVerbTargetCommand (verb.caster.Faction). The postfix
///     mirrors the vanilla repair, so the swapped-in verb is always
///     gizmo-safe. This crash is not MP-specific; MP only replays the click
///     everywhere.
///     MVCF: the same hand-built verb never receives a ManagedVerb, so on the
///     next load VerbManager.AddVerb logs "[MVCF] Attempted to get
///     ManagedVerb ..." and throws, aborting PostLoadInit for the whole mech
///     ("Could not do PostLoadInit on Wolfein_Mechanoid..."). The postfix
///     initializes the new primary verb through MVCF itself, and the AddVerb
///     guard keeps any other uninitialized verb from ever crashing a load
///     again. Everything is def/comp/instance based, so all clients reach the
///     same decision. MVCF parts are no-ops without MVCF.
/// </summary>
public static class WolfeinRaceGFIExpandManagedVerbs
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Managed Verbs Patch]";

    private const string FireModeSwitchTypeName = "JL_WolfeinExpand.CompWeaponFireModeSwitch";
    private const string ManagedVerbUtilityTypeName = "MVCF.Utilities.ManagedVerbUtility";
    private const string VerbManagerTypeName = "MVCF.VerbManager";

    private static MethodInfo managedGetter;
    private static MethodInfo initializeManagedMethod;
    private static FieldInfo equippableTrackerField;
    private static PropertyInfo managerPawnProperty;

    private static bool active;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchSwitchFireMode();
        PatchAddVerbGuard();

        Log.Message($"{LogPrefix} Initialized.");
    }

    // Always applied: needs only the GFI type.
    private static void PatchSwitchFireMode()
    {
        var fireModeSwitchType = AccessTools.TypeByName(FireModeSwitchTypeName);

        if (fireModeSwitchType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {FireModeSwitchTypeName}.");
            return;
        }

        var switchFireMode = AccessTools.Method(
                                 fireModeSwitchType,
                                 "SwitchFireMode",
                                 new[] { typeof(bool) })
                             ?? AccessTools.Method(
                                 fireModeSwitchType,
                                 "SwitchFireMode");

        if (switchFireMode == null)
        {
            Log.Warning($"{LogPrefix} Could not find {FireModeSwitchTypeName}.SwitchFireMode.");
            return;
        }

        MpCompat.harmony.Patch(
            switchFireMode,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandManagedVerbs),
                nameof(SwitchFireModePostfix)));
        Log.Message($"{LogPrefix} Patched {FireModeSwitchTypeName}.SwitchFireMode().");
    }

    // MVCF-gated: silently disabled when MVCF or its members are absent.
    private static void PatchAddVerbGuard()
    {
        if (!CacheMembers())
        {
            Log.Message($"{LogPrefix} MVCF not present, MVCF guards disabled.");
            return;
        }

        var addVerb = AccessTools.Method(
            AccessTools.TypeByName(VerbManagerTypeName),
            "AddVerb",
            new[] { typeof(Verb), AccessTools.TypeByName("MVCF.VerbSource") });

        if (addVerb == null)
        {
            Log.Warning($"{LogPrefix} Could not find MVCF.VerbManager.AddVerb, guard disabled.");
            return;
        }

        MpCompat.harmony.Patch(
            addVerb,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandManagedVerbs),
                nameof(AddVerbPrefix)));
        Log.Message($"{LogPrefix} Patched MVCF.VerbManager.AddVerb().");
    }

    private static bool CacheMembers()
    {
        var utilityType = AccessTools.TypeByName(ManagedVerbUtilityTypeName);
        var managerType = AccessTools.TypeByName(VerbManagerTypeName);

        if (utilityType == null || managerType == null)
            return false;

        managedGetter = AccessTools.Method(utilityType, "Managed", new[] { typeof(Verb), typeof(bool) });
        initializeManagedMethod =
            AccessTools.Method(utilityType, "InitializeManaged", new[] { typeof(Verb), typeof(VerbTracker) });
        equippableTrackerField = AccessTools.Field(typeof(CompEquippable), "verbTracker");
        managerPawnProperty = AccessTools.Property(managerType, "Pawn");

        if (managedGetter == null || initializeManagedMethod == null || equippableTrackerField == null)
            return false;

        active = true;
        return true;
    }

    // Runs inside the already-synced SwitchFireMode replay on every client,
    // so each client repairs its own local verb instance identically.
    private static void SwitchFireModePostfix(ThingComp __instance)
    {
        var equippable = __instance?.parent?.TryGetComp<CompEquippable>();
        var verb = equippable?.PrimaryVerb;

        if (verb == null)
            return;

        EnsureCaster(verb, equippable);

        if (!active)
            return;

        try
        {
            if (managedGetter.Invoke(null, new object[] { verb, false }) != null)
                return;

            var tracker = equippableTrackerField.GetValue(equippable) as VerbTracker;

            if (tracker == null)
                return;

            initializeManagedMethod.Invoke(null, new object[] { verb, tracker });
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} ManagedVerb init failed, continuing: {exception.Message}");
        }
    }

    // Mirrors the vanilla Pawn_EquipmentTracker.ExposeData repair
    // (verb.caster = pawn on PostLoadInit): GFI copies PrimaryVerb.caster at
    // switch time, which is still null when the switch is re-applied during
    // load before that repair runs. Only fills from the actual equipment
    // holder, never invents state.
    private static void EnsureCaster(Verb verb, CompEquippable equippable)
    {
        if (verb == null || verb.caster != null || equippable?.parent == null)
            return;

        try
        {
            if (equippable.parent.ParentHolder is Pawn_EquipmentTracker equipmentTracker
                && equipmentTracker.pawn != null)
            {
                verb.caster = equipmentTracker.pawn;
                Log.Message($"{LogPrefix} Repaired null caster on {verb.verbProps?.label ?? verb.GetType().Name}.");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Caster repair failed, continuing: {exception.Message}");
        }
    }

    // Backstop for any other verb that reaches the manager without MVCF
    // state: initialize it when its tracker can be found, otherwise skip it
    // instead of crashing the whole pawn load.
    private static bool AddVerbPrefix(object __instance, Verb verb)
    {
        if (!active || verb == null)
            return true;

        try
        {
            if (managedGetter.Invoke(null, new object[] { verb, false }) != null)
                return true;

            var tracker = FindOwningTracker(__instance, verb);

            if (tracker == null)
                return false;

            initializeManagedMethod.Invoke(null, new object[] { verb, tracker });

            return managedGetter.Invoke(null, new object[] { verb, false }) != null;
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} AddVerb guard failed, running original: {exception.Message}");
            return true;
        }
    }

    private static VerbTracker FindOwningTracker(object manager, Verb verb)
    {
        Pawn pawn = null;

        try
        {
            if (managerPawnProperty != null)
                pawn = managerPawnProperty.GetValue(manager) as Pawn;
        }
        catch
        {
            return null;
        }

        if (pawn == null)
            return null;

        if (pawn.VerbTracker?.AllVerbs != null)
            foreach (var candidate in pawn.VerbTracker.AllVerbs)
                if (candidate == verb)
                    return pawn.VerbTracker;

        if (pawn.equipment?.AllEquipmentListForReading != null)
            foreach (var equipment in pawn.equipment.AllEquipmentListForReading)
            {
                var equippable = equipment?.TryGetComp<CompEquippable>();

                if (equippable == null)
                    continue;

                var tracker = equippableTrackerField.GetValue(equippable) as VerbTracker;

                if (tracker?.AllVerbs == null)
                    continue;

                foreach (var candidate in tracker.AllVerbs)
                    if (candidate == verb)
                        return tracker;
            }

        return null;
    }
}