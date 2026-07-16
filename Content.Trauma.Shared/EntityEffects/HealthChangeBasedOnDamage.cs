// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that damages an entity based on the damage they have taken.
/// </summary>
public sealed partial class HealthChangeBasedOnDamage : EntityEffectBase<HealthChangeBasedOnDamage>
{
    /// <summary>
    /// Damage to deal, collected into entire damage groups.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public bool IgnoreResistances = true;

    /// <summary>
    /// The maximum damage we can have (per damage type).
    /// </summary>
    [DataField(required: true)]
    public float MaximumDamage;

    /// <summary>
    /// At which health amount should we stop the scaling? (e.g. at 100 health, we cap at <see cref="MaximumDamage"/>).
    /// </summary>
    [DataField]
    public float StopDamageScale = 100f;

    [DataField]
    public TargetBodyPart TargetBodyPart = TargetBodyPart.All;

    [DataField]
    public SplitDamageBehavior SplitDamage = SplitDamageBehavior.SplitEnsureAllOrganic;
}

public sealed partial class HealthChangeDamageScaleEffectSystem : EntityEffectSystem<DamageableComponent, HealthChangeBasedOnDamage>
{
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> ent, ref EntityEffectEvent<HealthChangeBasedOnDamage> args)
    {
        var damage = args.Effect.Damage;
        var maxDamage = args.Effect.MaximumDamage;
        var stopDamageAt = args.Effect.StopDamageScale;
        var scale = args.Scale;

        var damageToInflict = new DamageSpecifier(damage);

        foreach (var (type, amount) in _damageable.GetAllDamage(ent.AsNullable()).DamageDict)
        {
            if (!damage.DamageDict.ContainsKey(type))
                continue;

            var damageScale = FixedPoint2.Clamp(amount / stopDamageAt, 0f, 1f);
            var damageType = damage[type];

            var difference = maxDamage - damageType;
            var calculatedDamage = ( damageType + ( difference * damageScale ) ) * scale;

            // Checks to ensure it works for both positive and negative values (although why would you want to use this for positive values)
            if (calculatedDamage < maxDamage && damageType < 0)
                calculatedDamage = maxDamage;
            else if (calculatedDamage > maxDamage && damageType > 0)
                calculatedDamage = maxDamage;

            damageToInflict.DamageDict[type] = calculatedDamage;
        }

        _damageable.TryChangeDamage(
            ent.AsNullable(),
            damageToInflict,
            args.Effect.IgnoreResistances,
            targetPart: args.Effect.TargetBodyPart,
            splitDamage: args.Effect.SplitDamage,
            canMiss: false);
    }
}
