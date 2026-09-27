using System.Collections;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGfiExpandChip
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Chip Patch]";

    private const string CompChipSlotsName = "JL_WolfeinExpand.CompChipSlots";
    private const string CompLoadOutSlotsName = "JL_WolfeinExpand.CompLoadOutSlots";
    private const string ChipBillName = "JL_WolfeinExpand.ChipBill";
    private const string GetAllBillsMethodName = "GetAllBills";

    private static Type compChipSlotsType;
    private static Type compLoadOutSlotsType;
    private static Type chipBillType;

    private static MethodInfo chipSlotsGetAllBills;
    private static MethodInfo loadOutSlotsGetAllBills;

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        compChipSlotsType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, CompChipSlotsName);
        compLoadOutSlotsType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, CompLoadOutSlotsName);
        chipBillType = WolfeinRaceGfiExpandHelpers.GetTypeByName(LogPrefix, ChipBillName);

        if (compChipSlotsType != null)
        {
            chipSlotsGetAllBills = AccessTools.DeclaredMethod(
                compChipSlotsType,
                GetAllBillsMethodName,
                Type.EmptyTypes
            );
        }

        if (compLoadOutSlotsType != null)
        {
            loadOutSlotsGetAllBills = AccessTools.DeclaredMethod(
                compLoadOutSlotsType,
                GetAllBillsMethodName,
                Type.EmptyTypes
            );
        }

        RegisterChip();

        Log.Message($"{LogPrefix} Initialized.");
    }

    #region Registers

    private static void RegisterChip()
    {
        if (compChipSlotsType == null)
        {
            Log.Error($"{LogPrefix} Could not find {CompChipSlotsName}.");
            return;
        }

        if (compLoadOutSlotsType == null)
        {
            Log.Error($"{LogPrefix} Could not find {CompLoadOutSlotsName}.");
            return;
        }

        if (chipBillType == null)
        {
            Log.Error($"{LogPrefix} Could not find {ChipBillName}.");
            return;
        }

        RegisterMethods(compChipSlotsType);
        RegisterMethods(compLoadOutSlotsType);

        MP.RegisterSyncWorker<object>(
            SyncChipBill,
            chipBillType
        );
    }

    private static void RegisterMethods(Type componentType)
    {
        MP.RegisterSyncMethod(componentType, "AddInstallBill");
        MP.RegisterSyncMethod(componentType, "AddUninstallBill");
        MP.RegisterSyncMethod(componentType, "RemoveBill");
    }

    #endregion

    #region SyncChipBill

    private static void SyncChipBill(SyncWorker syncWorker, ref object bill)
    {
        if (syncWorker.isWriting)
        {
            var (owner, isLoadOut, billIndex) =
                FindChipBillOwner(bill);

            syncWorker.Write(owner);
            syncWorker.Write(isLoadOut);
            syncWorker.Write(billIndex);

            return;
        }

        var readPawn = syncWorker.Read<Pawn>();
        var readIsLoadOut = syncWorker.Read<bool>();
        var readBillIndex = syncWorker.Read<int>();

        bill = null;

        if (readPawn == null || readBillIndex < 0)
            return;

        var componentType = readIsLoadOut
            ? compLoadOutSlotsType
            : compChipSlotsType;

        var component = readPawn.AllComps?
            .FirstOrDefault(thingComp => thingComp.GetType() == componentType);

        if (component == null)
            return;

        var getAllBillsMethod = readIsLoadOut
            ? loadOutSlotsGetAllBills
            : chipSlotsGetAllBills;

        if (getAllBillsMethod == null)
            return;

        var billsResult = getAllBillsMethod.Invoke(component, null);

        if (billsResult is not IList bills)
            return;

        if (readBillIndex < 0 || readBillIndex >= bills.Count)
            return;

        bill = bills[readBillIndex];
    }

    private static (Pawn owner, bool isLoadOut, int index)
        FindChipBillOwner(object bill)
    {
        if (bill == null)
            return (null, false, -1);

        foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            if (pawn == null)
                continue;

            var chipComponent = pawn.AllComps?
                .FirstOrDefault(thingComp => thingComp.GetType() == compChipSlotsType);

            if (chipComponent != null)
            {
                var chipBills = GetBills(
                    chipComponent,
                    chipSlotsGetAllBills
                );

                var chipBillIndex = IndexOfReference(chipBills, bill);

                if (chipBillIndex >= 0)
                    return (pawn, false, chipBillIndex);
            }

            var loadOutComponent = pawn.AllComps?
                .FirstOrDefault(thingComp => thingComp.GetType() == compLoadOutSlotsType);

            if (loadOutComponent == null)
                continue;
            {
                var loadOutBills = GetBills(
                    loadOutComponent,
                    loadOutSlotsGetAllBills
                );

                var loadOutBillIndex = IndexOfReference(loadOutBills, bill);

                if (loadOutBillIndex >= 0)
                    return (pawn, true, loadOutBillIndex);
            }
        }

        Log.Warning($"{LogPrefix} Could not find the owner of the ChipBill being synchronized.");

        return (null, false, -1);
    }

    private static IList GetBills(
        ThingComp component,
        MethodInfo getAllBillsMethod)
    {
        if (component == null || getAllBillsMethod == null)
            return null;

        return getAllBillsMethod.Invoke(component, null) as IList;
    }

    private static int IndexOfReference(IList billList, object billItem)
    {
        if (billList == null || billItem == null)
            return -1;

        for (var billIndex = 0; billIndex < billList.Count; billIndex++)
            if (ReferenceEquals(billList[billIndex], billItem))
                return billIndex;

        return -1;
    }

    #endregion
}
