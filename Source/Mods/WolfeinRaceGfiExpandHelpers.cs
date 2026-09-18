using HarmonyLib;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGfiExpandHelpers
{
    #region Getters

    public static Type GetTypeByName(string _logPrefix, string _typeName)
    {
        var _type = AccessTools.TypeByName(_typeName);
        if (_type != null)
            return _type;

        Log.Warning($"{_logPrefix} Could not find {_typeName}.");

        return null;
    }

    #endregion
}