// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Projectiles;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that modifies the damage of a projectile
/// </summary>
public sealed partial class ModifyProjectileDamage : EntityEffectBase<ModifyProjectileDamage>
{
    /// <summary>
    /// How much to multiply the damage with.
    /// </summary>
    [DataField]
    public float Modifier = 1f;
}

public sealed class ModifyProjectileDamageEffectSystem : EntityEffectSystem<ProjectileComponent, ModifyProjectileDamage>
{
    protected override void Effect(Entity<ProjectileComponent> ent, ref EntityEffectEvent<ModifyProjectileDamage> args)
    {
        var effect = args.Effect;

        ent.Comp.Damage *= effect.Modifier * args.Scale;
        Dirty(ent);
    }
}
