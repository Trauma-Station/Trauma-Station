// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
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
/// In order to connect clockwork structures to a battery, they must have <see cref="ClockworkStructureComponent"/>.
///
/// The <see cref="ClockwinderComponent"/> is responsible for connecting a battery with a clockwork structure.
/// That means, any battery is able to be connected to a clockwork structure.
///
/// </summary>
public sealed class ClockworkPowerSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityQuery<PowerVeinComponent> _powerVeinQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkPowerSourceComponent, AnchorStateChangedEvent>(OnAnchored);
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
        if (_area.GetArea(xform.Coordinates) is not { } area || !_powerVeinQuery.HasComp(area))
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
            comp.RechargeDuration = ent.Comp.RechargeTime;

            ent.Comp.Active = true;

            Log.Debug("Activated power source");

            Dirty(ent.Owner, comp);
            Dirty(ent);
        }
    }
}
