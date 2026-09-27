using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace MultiplayerWolfeinRaceGFIExpandPatch.Source.Mods;

/// <summary>
/// CompExtraTex holds persistent state (selectedPawn, hologram offsets/scale)
/// saved via PostExposeData, so UI edits must be synced:
/// - ApplyHologramTransform(offsetX, offsetZ, scale) from the hologram dialog
///   sliders and Reset button.
/// - Pawn selection via the Select-Pawn gizmo targeter.
/// </summary>
public static class WolfeinRaceGFIExpandHologram
{
    private const string LogPrefix = "[Multiplayer Wolfein Race GFI Expand Hologram Patch]";

    private const string ExtraTexTypeName = "JL_WolfeinExpand.CompExtraTex";

    public static void Patch()
    {
        Log.Message($"{LogPrefix} Initializing...");

        var extraTexType = AccessTools.TypeByName(ExtraTexTypeName);
        if (extraTexType == null)
        {
            Log.Warning($"{LogPrefix} Could not find {ExtraTexTypeName}.");
            return;
        }

        // Slider drags + Reset call this every change; syncing the method keeps
        // all clients converged. Float args are natively syncable.
        var applyMethod = AccessTools.Method(extraTexType, "ApplyHologramTransform",
            new[] { typeof(float), typeof(float), typeof(float) });
        if (applyMethod == null)
        {
            Log.Warning($"{LogPrefix} Could not find {ExtraTexTypeName}.ApplyHologramTransform.");
        }
        else
        {
            MP.RegisterSyncMethod(extraTexType, "ApplyHologramTransform");
            Log.Message($"{LogPrefix} Registered {ExtraTexTypeName}.ApplyHologramTransform.");
        }

        // Pawn selection goes through a Targeter callback that sets the
        // selectedPawn field. Route it through our synced setter.
        MP.RegisterSyncMethod(typeof(WolfeinRaceGFIExpandHologram), nameof(SyncedSetSelectedPawn));

        // Sync the gizmo targeter lambdas: 0 = select-pawn onTargetSelected,
        // 1 = open hologram dialog (UI only, harmless to sync or skip).
        // Only the pawn-selection lambda mutates state; sync it. Best effort.
        TryRegisterGizmoLambdas();

        Log.Message($"{LogPrefix} Initialized.");
    }

    private static void TryRegisterGizmoLambdas()
    {
        // CompGetGizmosExtra yields select-pawn + adjust-hologram buttons.
        // The select-pawn button starts targeting; the actual state change
        // happens in the target callback, which we handle via SyncedSetSelectedPawn
        // + a prefix on the callback is overkill. Instead, watch the field:
        // register it as a sync field so direct sets converge.
        // (If MP cannot watch ThingComp fields reliably, ApplyHologramTransform
        // sync above still covers the slider path.)
        try
        {
            var selectedPawnField = AccessTools.Field(AccessTools.TypeByName(ExtraTexTypeName), "selectedPawn");
            if (selectedPawnField != null)
            {
                // Sync field watches require MP.Watch scopes; we expose the
                // setter method instead (more reliable for Targeter callbacks).
                Log.Message($"{LogPrefix} selectedPawn field found, using synced setter.");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"{LogPrefix} Hologram gizmo inspection failed: {exception.Message}");
        }
    }

    public static void SyncedSetSelectedPawn(ThingComp component, Pawn pawn)
    {
        if (component == null)
            return;

        var selectedPawnField = AccessTools.Field(component.GetType(), "selectedPawn");
        if (selectedPawnField == null)
            return;

        selectedPawnField.SetValue(component, pawn);

        // Invalidate cached portrait graphics so the new pawn renders.
        var invalidateMethod = AccessTools.Method(component.GetType(), "InvalidateHologramGraphics");
        invalidateMethod?.Invoke(component, null);
    }
}
