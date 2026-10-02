using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Syncs the Wolfein jet jump
///     (JL_WolfeinExpand.Verb_CastAbilityJumpWallPenetrating).
///     Vanilla Multiplayer auto-syncs OrderForceTarget only for verb types in
///     the RimWorld assembly. The GFI jump verb overrides OrderForceTarget on
///     itself (verified against Wolfein-Race-GFI-Expand-IL: public virtual
///     OrderForceTarget(LocalTargetInfo)), issuing a CastJump job with
///     verbToUse, so it must be registered explicitly - the same pattern as
///     WolfeinSprint in the base Wolfein patch project (different verb type,
///     no overlap). The rest of the chain (job execution, flyer ticks) is
///     pure sim and re-executes deterministically on all clients once the
///     order is synced.
/// </summary>
public static class WolfeinRaceGFIExpandAbilityJetJump
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Ability Jet Jump Patch]";

    private const string JetJumpVerbTypeName = "JL_WolfeinExpand.Verb_CastAbilityJumpWallPenetrating";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchOrderForceTarget();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void PatchOrderForceTarget()
    {
        var jumpVerbType = AccessTools.TypeByName(JetJumpVerbTypeName);

        if (jumpVerbType == null)
        {
            Log.Warning($"{LogPrefix} Type not found: {JetJumpVerbTypeName}.");
            return;
        }

        var method = AccessTools.DeclaredMethod(jumpVerbType, "OrderForceTarget", new[] { typeof(LocalTargetInfo) });

        if (method == null)
        {
            Log.Warning($"{LogPrefix} Could not find OrderForceTarget(LocalTargetInfo) for {JetJumpVerbTypeName}.");
            return;
        }

        MP.RegisterSyncMethod(method);
        Log.Message($"{LogPrefix} Synced {method.DeclaringType?.FullName}.OrderForceTarget.");
    }
}