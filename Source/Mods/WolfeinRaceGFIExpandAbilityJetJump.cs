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
        var jobExposeData = AccessTools.Method(typeof(Job), nameof(Job.ExposeData));

        if (jobExposeData == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "Verse.AI.Job.ExposeData().");
            return;
        }

        MpCompat.harmony.Patch(
            jobExposeData,
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
        var startNextToil = AccessTools.Method(typeof(JobDriver), "TryActuallyStartNextToil");

        if (startNextToil == null)
        {
            Log.Error($"{LogPrefix} Could not find " + "JobDriver.TryActuallyStartNextToil().");
            return;
        }

        MpCompat.harmony.Patch(
            startNextToil,
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

        var verb = __instance.verbToUse;

        if (!IsJetJumpVerb(verb))
            return;

        __instance.verbToUse = null;

        __state = new SavedVerbState
        {
            Job = __instance,
            Verb = verb
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

    private static void EnsureJetJumpVerb(JobDriver driver)
    {
        var job = driver.job;

        if (!IsCastJumpJob(job))
            return;

        if (job.verbToUse != null)
            return;

        var pawn = driver.pawn;
        if (pawn == null)
        {
            Log.Warning($"{LogPrefix} CastJump has no pawn.");
            return;
        }

        if (!HasJetJumpAbility(pawn))
            return;

        var jetJumpVerb = FindJetJumpVerb(pawn);
        if (jetJumpVerb == null)
        {
            Log.Warning($"{LogPrefix} Pawn has " + $"{JetJumpAbilityDefName}, but its Jet Jump verb was not found: " +
                        $"{pawn.LabelShort}.");
            return;
        }

        job.verbToUse = jetJumpVerb;
    }

    private static Verb FindJetJumpVerb(Pawn pawn)
    {
        if (pawn?.abilities?.abilities == null)
            return null;

        foreach (var ability in pawn.abilities.abilities)
        {
            if (ability == null)
                continue;

            if (ability.def?.defName != JetJumpAbilityDefName)
                continue;

            var verb = ability.verb;
            if (verb == null)
                continue;

            if (!IsJetJumpVerb(verb))
            {
                Log.Warning($"{LogPrefix} Ability " + $"{JetJumpAbilityDefName} has unexpected verb type " +
                            $"{verb.GetType().FullName}.");
                continue;
            }

            return verb;
        }

        return null;
    }

    private static bool HasJetJumpAbility(Pawn pawn)
    {
        if (pawn?.abilities?.abilities == null)
            return false;

        foreach (var ability in pawn.abilities.abilities)
            if (ability?.def?.defName == JetJumpAbilityDefName)
                return true;

        return false;
    }

    private static bool IsJetJumpVerb(Verb verb)
    {
        if (verb == null)
            return false;

        return verb.GetType().FullName == JetJumpVerbTypeName;
    }

    private static bool IsCastJumpJob(Job job)
    {
        return job?.def?.defName == CastJumpJobDefName;
    }

    private static bool IsCastJumpDriver(JobDriver driver)
    {
        return IsCastJumpJob(driver?.job);
    }

    private sealed class SavedVerbState
    {
        public Job Job;
        public Verb Verb;
    }
}
