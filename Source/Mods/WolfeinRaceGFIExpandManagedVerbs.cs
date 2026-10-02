using System.Reflection;
using HarmonyLib;
using Multiplayer.Compat;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     MVCF compatibility for verbs GFI builds outside VerbTracker.InitVerb.
///     CompWeaponFireModeSwitch.SwitchFireMode constructs a fresh secondary
///     verb and swaps it straight into the tracker list (verified in IL), so
///     MVCF never assigns it a ManagedVerb. On the next load MVCF
///     VerbManager.AddVerb then logs "[MVCF] Attempted to get ManagedVerb ..."
///     and throws NullReferenceException, aborting PostLoadInit for the whole
///     mech ("Could not do PostLoadInit on Wolfein_Mechanoid..."). Trigger
///     needs a fire-mode-switched gun (e.g. the auto-equipped sniper cannon)
///     equipped at save time plus MVCF active, which is why it only happens
///     sometimes; MP join-point reloads just make loads more frequent.
///     The postfix initializes the new primary verb through MVCF itself, and
///     the AddVerb guard keeps any other uninitialized verb from ever
///     crashing a load again. Everything is def/comp/instance based, so all
///     clients reach the same decision. No-op without MVCF.
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

        if (!CacheMembers())
        {
            Log.Message($"{LogPrefix} MVCF not present or members missing, skipping.");
            return;
        }

        var switchFireMode = AccessTools.Method(
                                 AccessTools.TypeByName(FireModeSwitchTypeName),
                                 "SwitchFireMode",
                                 new[] { typeof(bool) })
                             ?? AccessTools.Method(
                                 AccessTools.TypeByName(FireModeSwitchTypeName),
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

        Log.Message($"{LogPrefix} Initialized.");
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
    // so each client initializes its own local verb instance identically.
    private static void SwitchFireModePostfix(ThingComp __instance)
    {
        if (!active)
            return;

        var equippable = __instance?.parent?.TryGetComp<CompEquippable>();
        var verb = equippable?.PrimaryVerb;

        if (verb == null)
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