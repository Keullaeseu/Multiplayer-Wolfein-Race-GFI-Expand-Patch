using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGfiExpandHelpers
{
    #region Getters

    public static Type GetTypeByName(string logPrefix, string typeName)
    {
        var foundType = AccessTools.TypeByName(typeName);
        if (foundType != null)
            return foundType;

        Log.Warning($"{logPrefix} Could not find {typeName}.");

        return null;
    }

    #endregion
}
