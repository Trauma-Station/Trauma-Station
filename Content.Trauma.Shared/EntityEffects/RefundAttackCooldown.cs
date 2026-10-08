// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Weapons.Melee;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Gives the target back part of its own melee cooldown, it does not affect held weapons.
/// </summary>
public sealed partial class RefundAttackCooldown : EntityEffectBase<RefundAttackCooldown>
{
    [DataField]
    public float Fraction = 0.5f;
}

public sealed partial class RefundAttackCooldownSystem : EntityEffectSystem<MeleeWeaponComponent, RefundAttackCooldown>
{
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    protected override void Effect(Entity<MeleeWeaponComponent> ent, ref EntityEffectEvent<RefundAttackCooldown> args)
    {
        var rate = _melee.GetAttackRate(ent.Owner, ent.Owner, ent.Comp);
        if (rate <= 0f)
            return;

        ent.Comp.NextAttack -= TimeSpan.FromSeconds(args.Effect.Fraction * args.Scale / rate);
        DirtyField(ent.Owner, ent.Comp, nameof(MeleeWeaponComponent.NextAttack));
    }
}
