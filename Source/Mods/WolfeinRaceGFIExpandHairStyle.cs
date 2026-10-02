using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JL_WolfeinExpand;
using Multiplayer.API;
using Multiplayer.Compat;
using UnityEngine;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGFIExpandHairStyle
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Hair Style Patch]";

    private const string HairStyleSelectorName = "JL_WolfeinExpand.Dialog_HairStyleSelector";
    private const string WolfeinUiDialogName = "JL_WolfeinExpand.Dialog_WolfeinUI";

    private static FieldInfo savedHairStyleField;
    private static FieldInfo savedZoomLevelField;
    private static MethodInfo setHairStyleMethod;
    private static MethodInfo setZoomLevelMethod;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        savedHairStyleField = AccessTools.Field(typeof(CompWolfeinUI), "savedHairStyle");
        savedZoomLevelField = AccessTools.Field(typeof(CompWolfeinUI), "savedZoomLevel");

        if (savedHairStyleField == null)
        {
            Log.Error($"{LogPrefix} CompWolfeinUI.savedHairStyle was not found.");
            return;
        }

        if (savedZoomLevelField == null)
            Log.Warning($"{LogPrefix} CompWolfeinUI.savedZoomLevel was not found, zoom sync disabled.");

        setHairStyleMethod = AccessTools.Method(typeof(WolfeinRaceGFIExpandHairStyle), nameof(SetHairStyle));
        setZoomLevelMethod = AccessTools.Method(typeof(WolfeinRaceGFIExpandHairStyle), nameof(SetZoomLevel));

        MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandHairStyle), nameof(SetHairStyle));
        if (setZoomLevelMethod != null)
            MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandHairStyle), nameof(SetZoomLevel));

        PatchHairSelector();
        PatchWolfeinUiZoom();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void PatchHairSelector()
    {
        var dialogType = WolfeinRaceGFIExpandHelpers.GetTypeByName(LogPrefix, HairStyleSelectorName);
        if (dialogType == null)
            return;

        var doWindowContents = AccessTools.Method(dialogType, "DoWindowContents", new[] { typeof(Rect) });

        if (doWindowContents == null)
        {
            Log.Error($"{LogPrefix} Hair selector DoWindowContents was not found.");
            return;
        }

        MpCompat.harmony.Patch(
            doWindowContents,
            transpiler: new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandHairStyle),
                nameof(DoWindowContentsTranspiler)));

        Log.Message($"{LogPrefix} Patched hair selector DoWindowContents.");
    }

    private static void PatchWolfeinUiZoom()
    {
        if (savedZoomLevelField == null || setZoomLevelMethod == null)
            return;

        var dialogType = WolfeinRaceGFIExpandHelpers.GetTypeByName(LogPrefix, WolfeinUiDialogName);
        if (dialogType == null)
            return;

        var doWindowContents = AccessTools.Method(dialogType, "DoWindowContents", new[] { typeof(Rect) });
        if (doWindowContents == null)
        {
            Log.Warning($"{LogPrefix} WolfeinUI DoWindowContents was not found, zoom sync disabled.");
            return;
        }

        MpCompat.harmony.Patch(
            doWindowContents,
            transpiler: new HarmonyMethod(
                typeof(WolfeinRaceGFIExpandHairStyle),
                nameof(ZoomTranspiler)));

        Log.Message($"{LogPrefix} Patched WolfeinUI zoom assignments.");
    }

    public static void SetHairStyle(CompWolfeinUI comp, string hairStyle)
    {
        if (comp == null)
            return;

        comp.savedHairStyle = hairStyle;

        if (comp.parent is Pawn pawn)
            pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    public static void SetZoomLevel(CompWolfeinUI comp, float zoom)
    {
        if (comp == null)
            return;

        comp.savedZoomLevel = Mathf.Clamp(zoom, 0.5f, 2f);
    }

    private static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var replaced = false;

        foreach (var instruction in instructions)
            if (!replaced &&
                instruction.opcode == OpCodes.Stfld &&
                instruction.operand is FieldInfo field &&
                field == savedHairStyleField)
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    setHairStyleMethod);

                replaced = true;
            }
            else
            {
                yield return instruction;
            }

        if (!replaced)
            Log.Error($"{LogPrefix} Could not replace savedHairStyle assignment.");
    }

    private static IEnumerable<CodeInstruction> ZoomTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var replaced = 0;

        foreach (var instruction in instructions)
            if (instruction.opcode == OpCodes.Stfld &&
                instruction.operand is FieldInfo field &&
                field == savedZoomLevelField)
            {
                yield return new CodeInstruction(OpCodes.Call, setZoomLevelMethod);
                replaced++;
            }
            else
            {
                yield return instruction;
            }

        if (replaced == 0)
            Log.Warning($"{LogPrefix} Could not replace savedZoomLevel assignments.");
        else
            Log.Message($"{LogPrefix} Replaced {replaced} savedZoomLevel assignments.");
    }
}