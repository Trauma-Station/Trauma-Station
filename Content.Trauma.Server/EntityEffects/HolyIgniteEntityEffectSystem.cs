// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Shared.Religion;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

/// <summary>
/// This raises an the Ignite event on a given entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyIgniteEntityEffectSystem : EntityEffectSystem<WeakToHolyComponent, HolyIgnite>
{
    protected override void Effect(Entity<WeakToHolyComponent> entity, ref EntityEffectEvent<HolyIgnite> args)
    {
        var ev = new HolyIgniteEvent
        {
            FireStacksAdjustment = args.Effect.Stacks,
        };
        RaiseLocalEvent(entity, ref ev);
    }
}
