using HarmonyLib;
using Multiplayer.Compat;
using Verse;
using Verse.AI;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandAbilityJetJumpPatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Ability Jet Jump Patch]";

    private const string CastJumpJobDefName = "CastJump";
    private const string JetJumpAbilityDefName = "Wofleur_LongjumpMechLauncher";
    private const string JetJumpVerbTypeName = "JL_WolfeinExpand.Verb_CastAbilityJumpWallPenetrating";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchJobExposeData();
        PatchStartNextToil();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void PatchJobExposeData()
    {
        var _jobExposeData = AccessTools.Method(typeof(Job), nameof(Job.ExposeData));

        if (_jobExposeData == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "Verse.AI.Job.ExposeData().");
            return;
        }

        MpCompat.harmony.Patch(
            _jobExposeData,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandAbilityJetJumpPatch),
                nameof(JobExposeDataPrefix)),
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandAbilityJetJumpPatch),
                nameof(JobExposeDataPostfix)));

        Log.Message($"{LogPrefix} Patched Verse.AI.Job.ExposeData().");
    }

    private static void PatchStartNextToil()
    {
        var _startNextToil = AccessTools.Method(typeof(JobDriver), "TryActuallyStartNextToil");

        if (_startNextToil == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "JobDriver.TryActuallyStartNextToil().");
            return;
        }

        MpCompat.harmony.Patch(
            _startNextToil,
            new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandAbilityJetJumpPatch),
                nameof(TryActuallyStartNextToilPrefix)));

        Log.Message($"{LogPrefix} Patched " + "Verse.AI.JobDriver.TryActuallyStartNextToil().");
    }

    private static void JobExposeDataPrefix(Job __instance, out SavedVerbState __state)
    {
        __state = null;

        if (!IsCastJumpJob(__instance))
            return;

        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        var _verb = __instance.verbToUse;

        if (!IsJetJumpVerb(_verb))
            return;

        __instance.verbToUse = null;

        __state = new SavedVerbState
        {
            Job = __instance,
            Verb = _verb
        };
    }

    private static void JobExposeDataPostfix(SavedVerbState __state)
    {
        if (Scribe.mode != LoadSaveMode.Saving)
            return;

        if (__state?.Job != null)
            __state.Job.verbToUse = __state.Verb;
    }

    private static void TryActuallyStartNextToilPrefix(JobDriver __instance)
    {
        if (!IsCastJumpDriver(__instance))
            return;

        EnsureJetJumpVerb(__instance);
    }

    private static void EnsureJetJumpVerb(JobDriver _driver)
    {
        var _job = _driver.job;

        if (!IsCastJumpJob(_job))
            return;

        if (_job.verbToUse != null)
            return;

        var _pawn = _driver.pawn;
        if (_pawn == null)
        {
            Log.Warning($"{LogPrefix} CastJump has no pawn.");
            return;
        }

        if (!HasJetJumpAbility(_pawn))
            return;

        var _jetJumpVerb = FindJetJumpVerb(_pawn);
        if (_jetJumpVerb == null)
        {
            Log.Warning($"{LogPrefix} Pawn has " + $"{JetJumpAbilityDefName}, but its Jet Jump verb was not found: " +
                        $"{_pawn.LabelShort}.");
            return;
        }

        _job.verbToUse = _jetJumpVerb;
    }

    private static Verb FindJetJumpVerb(Pawn _pawn)
    {
        if (_pawn?.abilities?.abilities == null)
            return null;

        foreach (var _ability in _pawn.abilities.abilities)
        {
            if (_ability == null)
                continue;

            if (_ability.def?.defName != JetJumpAbilityDefName)
                continue;

            var _verb = _ability.verb;
            if (_verb == null)
                continue;

            if (!IsJetJumpVerb(_verb))
            {
                Log.Warning($"{LogPrefix} Ability " + $"{JetJumpAbilityDefName} has unexpected verb type " +
                            $"{_verb.GetType().FullName}.");
                continue;
            }

            return _verb;
        }

        return null;
    }

    private static bool HasJetJumpAbility(Pawn _pawn)
    {
        if (_pawn?.abilities?.abilities == null)
            return false;

        foreach (var _ability in _pawn.abilities.abilities)
            if (_ability?.def?.defName == JetJumpAbilityDefName)
                return true;

        return false;
    }

    private static bool IsJetJumpVerb(Verb _verb)
    {
        if (_verb == null)
            return false;

        return _verb.GetType().FullName == JetJumpVerbTypeName;
    }

    private static bool IsCastJumpJob(Job _job)
    {
        return _job?.def?.defName == CastJumpJobDefName;
    }

    private static bool IsCastJumpDriver(JobDriver _driver)
    {
        return IsCastJumpJob(_driver?.job);
    }

    private sealed class SavedVerbState
    {
        public Job Job;
        public Verb Verb;
    }
}