// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.TelescopicBaton;

// This is so heavily edited by Goobstation that I won't even bother commenting. It's not like we upstream from EE anyway.
public sealed class TelescopicBatonSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelescopicBatonComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<TelescopicBatonComponent, KnockdownOnHitAttemptEvent>(OnKnockdownAttempt);
        SubscribeLocalEvent<TelescopicBatonComponent, MeleeHitEvent>(OnMeleeHit, after: [typeof(KnockdownOnHitSystem)]);
    }

    private void OnMeleeHit(Entity<TelescopicBatonComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.AlwaysDropItems)
        {
            ent.Comp.NextAttack = TimeSpan.Zero;
            Dirty(ent);
        }

        if (args is { IsHit: true, HitEntities.Count: > 0 } && TryComp(ent, out UseDelayComponent? delay))
            _delay.ResetAllDelays((ent, delay));
    }

    private void OnToggled(Entity<TelescopicBatonComponent> baton, ref ItemToggledEvent args)
    {
        baton.Comp.NextAttack = args.Activated
            ? _timing.CurTime + baton.Comp.AttackTimeframe
            : TimeSpan.Zero;
        Dirty(baton);
    }

    private void OnKnockdownAttempt(Entity<TelescopicBatonComponent> baton, ref KnockdownOnHitAttemptEvent args)
    {
        if (!_toggle.IsActivated(baton.Owner))
            args.Cancelled = true;
        else
            args.DropItems = baton.Comp.NextAttack > _timing.CurTime;
    }
}
