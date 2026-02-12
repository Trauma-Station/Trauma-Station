// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Weapons;
using Content.Shared.Body;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable.Components;

namespace Content.Medical.Shared.Weapons;

/// <summary>
/// Attacking with damaged limbs/bones has a chance to fail
/// </summary>
public sealed class FumbleOnAttackSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private EntityQuery<WieldableComponent> _wieldQuery;

    public override void Initialize()
    {
        base.Initialize();

        _wieldQuery = GetEntityQuery<WieldableComponent>();

        SubscribeLocalEvent<HandsComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<HandsComponent, ShotAttemptedEvent>(OnAttemptShoot);
    }

    private void OnAttemptMelee(Entity<HandsComponent> ent, ref AttemptMeleeEvent args)
    {
        if (_hands.GetActiveHand(ent.AsNullable()) is not {} hand)
            return;

        var wielded = _wieldQuery.CompOrNull(args.Weapon)?.Wielded == true;
        var ev = new AttemptHandsMeleeEvent();
        foreach (var part in _body.GetOrgans<HandOrganComponent>(ent.Owner))
        {
            // raise on all hands if wielded
            if (!wielded && part.Comp.HandID != hand)
                continue;

            RaiseLocalEvent(part, ref ev);
            if (ev.Cancelled)
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    private void OnAttemptShoot(Entity<HandsComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Used.Owner == ent.Owner) // If the gun is the same user with a component e.g. laser eyes, dont bother.
            return;

        var wielded = _wieldQuery.CompOrNull(args.Used)?.Wielded == true;
        var hand = _hands.GetActiveHand(ent.AsNullable());
        var ev = new AttemptHandsShootEvent();
        foreach (var part in _body.GetOrgans<HandOrganComponent>(ent.Owner))
        {
            // raise on all hands if wielded
            if (!wielded && part.Comp.HandID != hand)
                continue;

            RaiseLocalEvent(part, ref ev);
            if (ev.Cancelled)
            {
                args.Cancel();
                return;
            }
        }
    }
}
