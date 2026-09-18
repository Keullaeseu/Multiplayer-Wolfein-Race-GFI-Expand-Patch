using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JL_WolfeinExpand;
using Multiplayer.API;
using Multiplayer.Compat;
using UnityEngine;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandHairStylePatch
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Hair Style Patch]";

    private const string HairStyleSelectorName = "JL_WolfeinExpand.Dialog_HairStyleSelector";

    private static FieldInfo savedHairStyleField;
    private static MethodInfo setHairStyleMethod;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var _dialogType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, HairStyleSelectorName);
        savedHairStyleField = AccessTools.Field(typeof(CompWolfeinUI), "savedHairStyle");

        if (savedHairStyleField == null)
        {
            Log.Error($"{LogPrefix} CompWolfeinUI.savedHairStyle was not found.");
            return;
        }

        setHairStyleMethod = AccessTools.Method(typeof(WolfeinRaceGFIExpandHairStylePatch), nameof(SetHairStyle));

        MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandHairStylePatch), nameof(SetHairStyle));

        var _doWindowContents = AccessTools.Method(_dialogType, "DoWindowContents", new[] { typeof(Rect) });

        if (_doWindowContents == null)
        {
            Log.Error($"{LogPrefix} DoWindowContents was not found.");
            return;
        }

        MpCompat.harmony.Patch(
            _doWindowContents,
            transpiler: new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandHairStylePatch),
                nameof(DoWindowContentsTranspiler)));

        Log.Message($"{LogPrefix} Initialized.");
    }

    public static void SetHairStyle(CompWolfeinUI _comp, string _hairStyle)
    {
        if (_comp == null)
            return;

        _comp.savedHairStyle = _hairStyle;

        if (_comp.parent is Pawn _pawn) _pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    private static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> _instructions)
    {
        var _replaced = false;

        foreach (var _instruction in _instructions)
            if (!_replaced &&
                _instruction.opcode == OpCodes.Stfld &&
                _instruction.operand is FieldInfo _field &&
                _field == savedHairStyleField)
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    setHairStyleMethod);

                _replaced = true;
            }
            else
            {
                yield return _instruction;
            }

        if (!_replaced)
            Log.Error($"{LogPrefix} Could not replace savedHairStyle assignment.");
    }
}