using Content.Shared.FixedPoint;
using Content.Shared._White.Xenomorphs;
using Content.Shared._White.Xenomorphs.Plasma;
using Content.Shared._White.Xenomorphs.Plasma.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;

namespace Content.Shared._White.Actions;

public sealed class PlasmaCostActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlasmaCostActionComponent, ActionRelayedEvent<PlasmaAmountChangeEvent>>(OnPlasmaAmountChange);
        SubscribeLocalEvent<PlasmaCostActionComponent, ActionAttemptEvent>(OnActionAttempt);
        SubscribeLocalEvent<PlasmaCostActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    /// <summary>
    /// Checks if the performer has enough plasma for the action.
    /// Returns true if the action should proceed, false if it should be blocked.
    /// </summary>
    public bool HasEnoughPlasma(EntityUid performer, FixedPoint2 cost)
    {
        if (cost <= 0)
            return true;

        return TryComp<PlasmaVesselComponent>(performer, out var plasmaVessel) &&
               plasmaVessel.Plasma >= cost;
    }

    /// <summary>
    /// Deducts plasma from the performer. Call this after confirming the action succeeds.
    /// </summary>
    public void DeductPlasma(EntityUid performer, FixedPoint2 cost)
    {
        if (cost > 0)
            _plasma.ChangePlasmaAmount(performer, -cost);
    }

    private void OnPlasmaAmountChange(EntityUid uid, PlasmaCostActionComponent component, ActionRelayedEvent<PlasmaAmountChangeEvent> args)
    {
        _actions.SetEnabled(uid, component.PlasmaCost <= args.Args.Amount);
    }

    private void OnActionAttempt(Entity<PlasmaCostActionComponent> ent, ref ActionAttemptEvent args)
    {
        if (!_plasma.HasPlasma(args.User, ent.Comp.PlasmaCost))
            args.Cancelled = true;
    }

    private void OnActionPerformed(Entity<PlasmaCostActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (ent.Comp.Immediate)
            DeductPlasma(args.Performer, ent.Comp.PlasmaCost);
    }
}
