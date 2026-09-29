// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Singularity.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Effects;

namespace Content.Trauma.Server.EntityEffects.Effects;

/// <summary>
/// Effect that shoot lightnings(like from tesla) from the target entity
/// </summary>
public sealed partial class GravPulseEffectSystem : EntityEffectSystem<TransformComponent, GravPulse>
{
    [Dependency] private GravityWellSystem _gravWell = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<GravPulse> args)
    {
        _gravWell.GravPulse(ent,
            args.Effect.MaxRange,
            args.Effect.MinRange,
            args.Effect.BaseRadialAcceleration,
            args.Effect.BaseTangentialAcceleration);
    }
}
