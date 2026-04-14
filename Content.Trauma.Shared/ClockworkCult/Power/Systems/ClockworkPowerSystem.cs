// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
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
/// 1. Add support for checking for distance and possibly check if obstructed (e.g. can't connect a power source to a structure if its too far away)
/// Test stuff i added
///
/// </summary>
public sealed class ClockworkPowerSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClockworkPowerTransferSystem _powerTransfer = default!;
    [Dependency] private readonly EntityQuery<PowerVeinComponent> _powerVeinQuery = default!;
    [Dependency] private readonly EntityQuery<ClockwinderComponent> _clockwinderQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkPowerSourceComponent, AnchorStateChangedEvent>(OnAnchored);
        SubscribeLocalEvent<ClockworkStructureComponent, AnchorStateChangedEvent>(OnStructureAnchored);

        SubscribeLocalEvent<ClockworkStructureComponent, ClockwinderInteractEvent>(OnClockwinder);
        SubscribeLocalEvent<ClockworkStructureComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    /// <summary>
    /// Handles activating the clockwork power source entity, or de-activating it, based on if its anchored or not.
    /// </summary>
    private void OnAnchored(Entity<ClockworkPowerSourceComponent> ent, ref AnchorStateChangedEvent args)
    {
        // We are un-anchoring it while its active, so remove the Self Recharging component
        if (!args.Anchored && ent.Comp.Active)
        {
            if (_timing.ApplyingState)
                return;

            // TODO:
            // Make it so you can lock anchor them in place,
            // since removing this comp will result in losing all charges
            // (not intended, but this can't act as storage so its good lol)
            RemCompDeferred<AutoRechargeComponent>(ent.Owner);
            ent.Comp.Active = false;

            Dirty(ent);

            Log.Debug("De-activated power source");
            return;
        }

        // A clockwork power source must always sit on top of a vein to activate
        var xform = Transform(ent.Owner);
        if (_area.GetArea(xform.Coordinates) is not { } area || !_powerVeinQuery.TryComp(area, out var vein))
        {
            Log.Debug("Not standing on vein");
            return;
        }

        // Using battery self recharger since it's better than writing a system that does the same thing lol
        if (args.Anchored && !ent.Comp.Active)
        {
            if (_timing.ApplyingState)
                return;

            var comp = EnsureComp<AutoRechargeComponent>(ent.Owner);
            comp.RechargeDuration = ent.Comp.RechargeTime + vein.ReducedRechargeTime;

            ent.Comp.Active = true;

            Log.Debug("Activated power source");

            Dirty(ent.Owner, comp);
            Dirty(ent);
        }
    }

    private void OnStructureAnchored(Entity<ClockworkStructureComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored && ent.Comp.Active)
        {
            ent.Comp.Active = false;
            Dirty(ent);
        } else if (args.Anchored && !ent.Comp.Active)
        {
            ent.Comp.Active = true;
            Dirty(ent);
        }

        if (ent.Comp.ComponentsOnActivation is not { } components)
            return;

        if (_timing.ApplyingState)
            return;

        // Add components once we get activated
        if (ent.Comp.Active)
        {
            EntityManager.AddComponents(ent.Owner, components);
            return;
        }

        // Remove them once we get de-activated
        EntityManager.RemoveComponents(ent.Owner, components);
    }

    private void OnClockwinder(Entity<ClockworkStructureComponent> ent, ref ClockwinderInteractEvent args)
    {
        if (args.Transferrer is not {} transferrer)
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
}
