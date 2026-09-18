using System.Collections;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

public static class WolfeinRaceGfiExpandChipPatch
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

        chipSlotsGetAllBills = AccessTools.DeclaredMethod(
            compChipSlotsType,
            GetAllBillsMethodName,
            Type.EmptyTypes
        );

        loadOutSlotsGetAllBills = AccessTools.DeclaredMethod(
            compLoadOutSlotsType,
            GetAllBillsMethodName,
            Type.EmptyTypes
        );

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

    private static void RegisterMethods(Type _type)
    {
        MP.RegisterSyncMethod(_type, "AddInstallBill");
        MP.RegisterSyncMethod(_type, "AddUninstallBill");
        MP.RegisterSyncMethod(_type, "RemoveBill");
    }

    #endregion

    #region SyncChipBill

    private static void SyncChipBill(SyncWorker _syncWorker, ref object _bill)
    {
        if (_syncWorker.isWriting)
        {
            var (_owner, _isLoadOut, _index) =
                FindChipBillOwner(_bill);

            _syncWorker.Write(_owner);
            _syncWorker.Write(_isLoadOut);
            _syncWorker.Write(_index);

            return;
        }

        var _pawnRead = _syncWorker.Read<Pawn>();
        var _isLoadOutRead = _syncWorker.Read<bool>();
        var _indexRead = _syncWorker.Read<int>();

        _bill = null;

        if (_pawnRead == null || _indexRead < 0)
            return;

        var _componentType = _isLoadOutRead
            ? compLoadOutSlotsType
            : compChipSlotsType;

        var _component = _pawnRead.AllComps?
            .FirstOrDefault(_comp => _comp.GetType() == _componentType);

        if (_component == null)
            return;

        var _getAllBillsMethod = _isLoadOutRead
            ? loadOutSlotsGetAllBills
            : chipSlotsGetAllBills;

        if (_getAllBillsMethod == null)
            return;

        var _result = _getAllBillsMethod.Invoke(_component, null);

        if (_result is not IList _bills)
            return;

        if (_indexRead < 0 || _indexRead >= _bills.Count)
            return;

        _bill = _bills[_indexRead];
    }

    private static (Pawn owner, bool isLoadOut, int index)
        FindChipBillOwner(object _bill)
    {
        if (_bill == null)
            return (null, false, -1);

        foreach (var _pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive)
        {
            if (_pawn == null)
                continue;

            var _chipComponent = _pawn.AllComps?
                .FirstOrDefault(_comp => _comp.GetType() == compChipSlotsType);

            if (_chipComponent != null)
            {
                var _bills = GetBills(
                    _chipComponent,
                    chipSlotsGetAllBills
                );

                var _index = IndexOfReference(_bills, _bill);

                if (_index >= 0)
                    return (_pawn, false, _index);
            }

            var _loadOutComponent = _pawn.AllComps?
                .FirstOrDefault(_comp => _comp.GetType() == compLoadOutSlotsType);

            if (_loadOutComponent == null)
                continue;
            {
                var _bills = GetBills(
                    _loadOutComponent,
                    loadOutSlotsGetAllBills
                );

                var _index = IndexOfReference(_bills, _bill);

                if (_index >= 0)
                    return (_pawn, true, _index);
            }
        }

        Log.Warning($"{LogPrefix} Could not find the owner of the ChipBill being synchronized.");

        return (null, false, -1);
    }

    private static IList GetBills(
        ThingComp _component,
        MethodInfo _getAllBillsMethod)
    {
        if (_component == null || _getAllBillsMethod == null)
            return null;

        return _getAllBillsMethod.Invoke(_component, null) as IList;
    }

    private static int IndexOfReference(IList _list, object _item)
    {
        if (_list == null || _item == null)
            return -1;

        for (var _index = 0; _index < _list.Count; _index++)
            if (ReferenceEquals(_list[_index], _item))
                return _index;

        return -1;
    }

    #endregion
}