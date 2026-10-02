using System.Reflection;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Locates compiler-generated lambdas by name prefix plus signature
///     instead of ordinal numbers. Ordinals and generated type layouts shift
///     between mod builds (display class vs methods directly on the type),
///     while the <c>&lt;ParentMethod&gt;b__</c> naming plus the signature
///     stay put. Verified against the mod IL
///     (Wolfein-Race-GFI-Expand-IL).
///     Lambdas declared directly on the parent type are synced with
///     MP.RegisterSyncMethod: their instance is the syncable comp, hediff or
///     thing itself, so no delegate field sync is needed.
/// </summary>
internal static class WolfeinRaceGFIExpandLambdaSync
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Patch]";

    // All lambdas of parentMethod declared directly on parentType with an
    // exact signature match. Getters (different return type) never match.
    public static List<MethodInfo> FindParentLambdas(Type parentType, string parentMethod, Type returnType,
        params Type[] argTypes)
    {
        var matches = new List<MethodInfo>();
        var prefix = "<" + parentMethod + ">b__";

        foreach (var candidate in parentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                        BindingFlags.Instance | BindingFlags.Static |
                                                        BindingFlags.DeclaredOnly))
        {
            if (!candidate.Name.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            if (candidate.ReturnType != returnType)
                continue;

            var parameters = candidate.GetParameters();

            if (parameters.Length != argTypes.Length)
                continue;

            var shapeMatches = true;

            for (var index = 0; index < parameters.Length; index++)
                if (parameters[index].ParameterType != argTypes[index])
                {
                    shapeMatches = false;
                    break;
                }

            if (shapeMatches)
                matches.Add(candidate);
        }

        return matches;
    }

    // Syncs every match. Warns without throwing when nothing matches, so a
    // mod update can never break the remaining patches (WolfeinRaceGFIExpand
    // additionally isolates each feature with SafePatch).
    public static int SyncParentLambdas(Type parentType, string parentMethod, Type returnType, params Type[] argTypes)
    {
        var matches = FindParentLambdas(parentType, parentMethod, returnType, argTypes);

        foreach (var match in matches)
        {
            MP.RegisterSyncMethod(match);
            Log.Message($"{LogPrefix} Synced {parentType.Name}.{parentMethod} action {match.Name}.");
        }

        if (matches.Count == 0)
            Log.Warning($"{LogPrefix} No matching actions found in {parentType.Name}.{parentMethod}.");

        return matches.Count;
    }
}