// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
/// This handles power related functions for cogcult.
///
/// Entities with <see cref="ClockworkPowerSourceComponent"/> are the ones who generate power,
/// if anchored on top of an entity with <see cref="PowerVeinComponent"/>.
///
/// In order to connect clockwork structures to a <see cref="ClockworkTransferrerComponent"/>, they must have <see cref="ClockworkStructureComponent"/>.
///
/// The <see cref="ClockwinderComponent"/> is responsible for connecting a structure with a transferrer.
///
/// TODO for finishing prototype:
/// Test stuff i added
///
/// </summary>
public sealed partial class ClockworkPowerSystem : EntitySystem
{
    [Dependency] private AreaSystem _area = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ClockworkPowerTransferSystem _powerTransfer = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<PowerVeinComponent> _powerVeinQuery = default!;
    [Dependency] private EntityQuery<ClockwinderComponent> _clockwinderQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkActivatableComponent, AnchorStateChangedEvent>(OnAnchored);

        SubscribeLocalEvent<ClockworkPowerSourceComponent, ClockworkStructureStateChangedEvent>(OnActiveChanged);

        SubscribeLocalEvent<ClockworkStructureComponent, ClockwinderInteractEvent>(OnClockwinder);
        SubscribeLocalEvent<ClockworkStructureComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<ClockworkStructureComponent, MoveEvent>(OnMove);
    }

    private void OnAnchored(Entity<ClockworkActivatableComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored && ent.Comp.Active)
        {
            ent.Comp.Active = false;
        }
        else if (args.Anchored && !ent.Comp.Active)
        {
            ent.Comp.Active = true;
        }
        Dirty(ent);

        var ev = new ClockworkStructureStateChangedEvent(ent.Comp.Active);
        RaiseLocalEvent(ent.Comp.Owner, ref ev);

        Log.Debug($"Structure has been activated: {ent.Comp.Active}");

        if (_timing.ApplyingState)
            return;

        // Add components once we get activated
        if (ent.Comp.Active)
        {
            if (ent.Comp.ActivationEffects is { } activationEffects)
                _effects.ApplyEffects(ent.Owner, activationEffects);
            return;
        }

        if (ent.Comp.DeactivationEffects is { } deactivationEffects)
            _effects.ApplyEffects(ent.Owner, deactivationEffects);
    }

    /// <summary>
    /// Handles the activation of clockwork power sources.
    /// </summary>
    private void OnActiveChanged(Entity<ClockworkPowerSourceComponent> ent,
        ref ClockworkStructureStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!args.Active)
        {
            // TODO: Make it so you can lock anchor them in place,
            RemCompDeferred<AutoRechargeComponent>(ent.Owner);
            return;
        }

        // In order to activate the power source, we must be standing on a power vein
        var xform = Transform(ent.Owner);
        if (_area.GetArea(xform.Coordinates) is not { } area || !_powerVeinQuery.TryComp(area, out var vein))
            return;

        var comp = EnsureComp<AutoRechargeComponent>(ent.Owner);
        comp.RechargeDuration = ent.Comp.RechargeTime + vein.ReducedRechargeTime;

        Dirty(ent.Owner, comp);
    }

    private void OnClockwinder(Entity<ClockworkStructureComponent> ent, ref ClockwinderInteractEvent args)
    {
        if (args.Transferrer is not { } transferrer)
            return;

        // Add the connection to the transferrer
        _powerTransfer.AddConnection(transferrer, ent.Owner);

        ent.Comp.Transferrers.Add(transferrer);
        Dirty(ent);
    }

    private void OnGetVerbs(Entity<ClockworkStructureComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract || args.Using is not {} itemUsing)
            return;

        if (!_clockwinderQuery.HasComp(itemUsing))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = "Clear Connection",
            IconEntity = GetNetEntity(itemUsing),
            Act = () =>
            {
                _powerTransfer.RemoveConnection(ent.AsNullable());
            }
        });
    }

    private void OnMove(Entity<ClockworkStructureComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.Transferrers.Count == 0)
            return;

        // Moving away should disconnect clockwork structures from their transferrers that aren't in 10 tile range
        foreach (var transferrer in ent.Comp.Transferrers)
        {
            if (TerminatingOrDeleted(transferrer) || transferrer is not { } transfer)
                continue;

            if (_transform.InRange(ent.Owner, transfer, 10f))
                continue;

            _powerTransfer.RemoveConnection(transfer, ent.Owner);
        }
    }
}


/// <summary>
/// Raised on self when a clockwork structure gets its Active state changed.
/// </summary>
[ByRefEvent]
public record struct ClockworkStructureStateChangedEvent(bool Active);
