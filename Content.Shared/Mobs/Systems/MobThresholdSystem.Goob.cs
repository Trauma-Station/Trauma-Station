using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Mobs.Systems;

/// <summary>
/// Goob - vital damage API.
/// </summary>
public sealed partial class MobThresholdSystem
{
    /// <summary>
    /// Calculates the total damage from vital body parts (Head, Chest, Groin), for complex-bodies.
    /// For non-complex bodies or if no vital parts are found, returns the total damage from the target entity.
    /// </summary>
    /// <param name="target">The entity to check for vital damage</param>
    /// <param name="damageableComponent">The damageable component of the target entity</param>
    /// <returns>Total damage from vital body parts, or total damage if not a complex body or no vital parts found</returns>
    public FixedPoint2 CheckVitalDamage(EntityUid target, DamageableComponent damageableComponent)
    {
        var damage = damageableComponent.TotalDamage;

        if (!_bodyQuery.TryComp(target, out var body))
            return damage;

        var result = FixedPoint2.Zero;
        foreach (var woundable in _body.GetVitalParts(target))
        {
            if (_damageQuery.TryComp(woundable, out var partDamage))
                result += partDamage.TotalDamage;
        }

        return result;
    }
}
