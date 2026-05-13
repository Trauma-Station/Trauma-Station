// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Trauma.Shared.Xenomorphs;
using Content.Trauma.Shared.Xenomorphs.Plasma;
using Content.Trauma.Shared.Xenomorphs.Plasma.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;

namespace Content.Trauma.Shared.Actions;

public sealed partial class PlasmaCostActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPlasmaSystem _plasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlasmaCostActionComponent, PlasmaAmountChangeEvent>(OnPlasmaAmountChange);
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

    private void OnPlasmaAmountChange(EntityUid uid, PlasmaCostActionComponent component, ref PlasmaAmountChangeEvent args)
    {
        _actions.SetEnabled(uid, component.PlasmaCost <= args.Amount);
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
