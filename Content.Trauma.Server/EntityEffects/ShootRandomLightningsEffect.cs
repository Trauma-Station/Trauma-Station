// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Lightning;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Effects;

namespace Content.Trauma.Server.EntityEffects.Effects;

/// <summary>
/// Effect that shoot lightnings(like from tesla) from the target entity
/// </summary>
public sealed partial class ShootRandomLightningsEffectSystem : EntityEffectSystem<TransformComponent, ShootRandomLightnings>
{
    [Dependency] private LightningSystem _lightning = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<ShootRandomLightnings> args)
    {
        _lightning.ShootRandomLightnings(ent, args.Effect.LightningRange, args.Effect.LightningBoltCount);
    }
}
