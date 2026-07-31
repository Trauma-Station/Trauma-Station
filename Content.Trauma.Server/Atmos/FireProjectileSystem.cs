// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Projectiles;

namespace Content.Trauma.Server.Atmos;

public sealed partial class FireProjectileSystem : EntitySystem
{
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private EntityQuery<FlammableComponent> _query = default!;

    [SubscribeLocalEvent]
    private void OnProjectileHit(Entity<IgniteOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        var otherEnt = args.Target;
        if (!_query.TryComp(otherEnt, out var flammable))
            return;

        flammable.FireStacks += ent.Comp.FireStacks;
        _flammable.Ignite(otherEnt, ent, flammable);
        ent.Comp.Count--;

        if (ent.Comp.Count == 0)
            RemCompDeferred<IgniteOnCollideComponent>(ent);
    }
}
