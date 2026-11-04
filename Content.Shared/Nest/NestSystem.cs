using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Nest;

public sealed partial class NestSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityConditionsSystem _condition = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [Dependency] private EntityQuery<NestComponent> _nestQuery = default!;

    [SubscribeLocalEvent]
    private void OnAttemptInsert(Entity<NestComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (_condition.TryConditions(args.EntityUid, ent.Comp.Conditions, ent))
            return;

        _popup.PopupEntity("The nest refuses the offering!", ent, PopupType.MediumCaution);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnInsert(Entity<NestComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ent.Comp.TargetEffects is { } targetEffects)
            _effects.ApplyEffects(args.Entity, targetEffects);

        var nest = ent.AsNullable();
        AdjustPoints(nest, ent.Comp.PointAdjust);

        // Check if we reached the trigger, if yes, run the effects and reset points
        if (!CheckTrigger(nest))
            return;

        _effects.ApplyEffects(ent, ent.Comp.Effects);
    }

    /// <summary>
    /// Adjusts points of the nest, and runs effects if the trigger has been reached.
    /// </summary>
    /// <param name="ent">The nest</param>
    /// <param name="amount">The amount to adjust</param>
    public void AdjustPoints(Entity<NestComponent?> ent, int amount)
    {
        if (!_nestQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.CurrentPoints = Math.Clamp(ent.Comp.CurrentPoints + amount, 0, ent.Comp.TriggerPoints);
        Dirty(ent);
    }

    /// <summary>
    /// Checks if the trigger has been met on the entity
    /// </summary>
    /// <param name="ent">The nest</param>
    /// <returns>True if the trigger was met, false otherwise</returns>
    public bool CheckTrigger(Entity<NestComponent?> ent)
    {
        if (!_nestQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (ent.Comp.CurrentPoints != ent.Comp.TriggerPoints)
            return false;

        ent.Comp.CurrentPoints = 0;
        Dirty(ent);

        return true;
    }
}
