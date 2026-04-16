// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Trauma.Common.Knockdown;

namespace Content.Goobstation.Shared.Knockdown;

public sealed class KnockdownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<KnockdownOnCollideComponent, ThrowDoHitEvent>(OnEntityHit);
    }

    private void OnEntityHit(Entity<KnockdownOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent);
    }

    private void OnProjectileHit(Entity<KnockdownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent);
    }

    private void ApplyEffects(EntityUid target, Entity<KnockdownOnCollideComponent> ent)
    {
        var ev = new KnockdownOnCollideAttemptEvent(ent.Owner);
        RaiseLocalEvent(target, ev);
        if (ev.Cancelled)
            return;

        _stun.TryKnockdown(target, time: null, drop: ent.Comp.DropItems);
    }
}
