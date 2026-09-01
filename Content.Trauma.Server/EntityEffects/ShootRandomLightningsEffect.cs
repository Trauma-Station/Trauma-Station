// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Lightning;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects.Effects;

public sealed partial class ShootRandomLightnings : EntityEffectBase<ShootRandomLightnings>
{
    /// <summary>
    /// Up to how far to teleport the user in tiles.
    /// </summary>
    [DataField]
    public float LightningRange = 5f;

    /// <summary>
    /// How many times to try to pick the destination. Larger number means the teleport is more likely to be safe.
    /// </summary>
    [DataField]
    public int LightningBoltCount = 5;
}

public sealed partial class ShootRandomLightningsEffectSystem : EntityEffectSystem<TransformComponent, ShootRandomLightnings>
{
    [Dependency] private LightningSystem _lightning = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<ShootRandomLightnings> args)
    {
        _lightning.ShootRandomLightnings(ent, args.Effect.LightningRange, args.Effect.LightningBoltCount);
    }
}
