using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
///     Syncs weapon gizmo actions:
///     - CompWeaponSwitch.SwitchWeapon (swaps to alternate weapon, moves equipment)
///     - CompWeaponFireModeSwitch.SwitchFireMode (swaps primary verb)
///     Registering the methods is sufficient: the gizmo lambdas call them from the
///     interface, and MP intercepts synced-method calls issued from the UI.
/// </summary>
public static class WolfeinRaceGFIExpandWeapon
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Weapon Patch]";

    private const string WeaponSwitchName = "JL_WolfeinExpand.CompWeaponSwitch";
    private const string FireModeSwitchName = "JL_WolfeinExpand.CompWeaponFireModeSwitch";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        PatchWeaponSwitch();
        PatchFireModeSwitch();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void PatchWeaponSwitch()
    {
        var weaponSwitchType = AccessTools.TypeByName(WeaponSwitchName);
        if (weaponSwitchType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {WeaponSwitchName}.");
            return;
        }

        var switchWeaponMethod = AccessTools.Method(weaponSwitchType, "SwitchWeapon");
        if (switchWeaponMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find {WeaponSwitchName}.SwitchWeapon.");
            return;
        }

        MP.RegisterSyncMethod(weaponSwitchType, "SwitchWeapon");
        Log.Message($"{LogPrefix} Registered {WeaponSwitchName}.SwitchWeapon.");
    }

    private static void PatchFireModeSwitch()
    {
        var fireModeSwitchType = AccessTools.TypeByName(FireModeSwitchName);
        if (fireModeSwitchType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {FireModeSwitchName}.");
            return;
        }

        var switchFireModeMethod = AccessTools.Method(fireModeSwitchType, "SwitchFireMode", new[] { typeof(bool) });
        if (switchFireModeMethod == null)
            switchFireModeMethod = AccessTools.Method(fireModeSwitchType, "SwitchFireMode");

        if (switchFireModeMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find {FireModeSwitchName}.SwitchFireMode.");
            return;
        }

        MP.RegisterSyncMethod(switchFireModeMethod);
        Log.Message($"{LogPrefix} Registered {FireModeSwitchName}.SwitchFireMode.");
    }
}