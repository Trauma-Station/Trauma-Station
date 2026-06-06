// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Mobs.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Components;

namespace Content.Trauma.Shared.TelescopicBaton;

public sealed partial class KnockdownOnHitSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MobStateSystem _mobState = default!; // Goobstation

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockdownOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<KnockdownOnHitComponent> entity, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any()) // Goob edit
            return;

        if (!entity.Comp.KnockdownOnHeavyAttack && args.Direction != null)
            return;

        var ev = new KnockdownOnHitAttemptEvent(false, false); // Goob edit
        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
            return;

        List<EntityUid> knockedDown = new(); // Goobstation
        foreach (var target in
                 args.HitEntities.Where(e => !HasComp<BorgChassisComponent>(e) && _mobState.IsAlive(e))) // Goob edit
        {
            if (_stun.TryKnockdown(target,
                entity.Comp.Duration,
                entity.Comp.RefreshDuration,
                drop: ev.DropItems))
                knockedDown.Add(target);
        }

        if (knockedDown.Count > 0) // Goobstation
            RaiseLocalEvent(entity, new KnockdownOnHitSuccessEvent(knockedDown));
    }
}
