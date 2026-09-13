// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Systems;

/// <summary>
/// Trauma - GetScaledDamage overload for polymorph transferring part damage
/// </summary>
public sealed partial class MobThresholdSystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;

    /// <summary>
    /// Version of GetScaledDamage that also gets the parts damage, indexed by organ category.
    /// </summary>
    public bool GetScaledDamage(
        EntityUid target1,
        EntityUid target2,
        out DamageSpecifier? damage,
        out Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier>? woundableDamage)
    {
        woundableDamage = null;
        if (!GetScaledDamage(target1, target2, out damage))
            return false;

        woundableDamage = GetScaledPartsDamage(target1, target2);
        return true;
    }

    /// <summary>
    /// Gets lowest state change threshold (softcrit/crit/dead)
    /// </summary>
    public FixedPoint2 GetLowestThreshold(Entity<MobThresholdsComponent?> target)
    {
        if (!TryGetThresholdForState(target, MobState.SoftCrit, out var threshold, target.Comp) &&
            !TryGetThresholdForState(target, MobState.Critical, out threshold, target.Comp) &&
            !TryGetThresholdForState(target, MobState.Dead, out threshold, target.Comp))
            threshold = 0;

        return threshold.Value;
    }

    private Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier>? GetScaledPartsDamage(EntityUid target1, EntityUid target2)
    {
        // If the receiver is a simplemob, we don't care about any of this. Just grab the damage and go.
        if (!_bodyQuery.HasComp(target2))
            return null;

        // However if they are valid for woundmed, we first check if the sender is also valid for it to build a dict.
        if (!_bodyQuery.TryComp(target1, out var oldBody))
            return null;

        var ent1DeadThreshold = GetLowestThreshold(target1);
        var ent2DeadThreshold = GetLowestThreshold(target2);

        Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier> organDamages = new();
        foreach (var organ in _body.GetOrgans((target1, oldBody)))
        {
            if (organ.Comp.Category is not {} category
                || !_damageQuery.TryComp(organ, out var damageable))
                continue;

            var damage = _damageable.GetAllDamage((organ, damageable));
            if (damage.GetTotal() <= 0)
                continue;

            var modifiedDamage = damage * ent2DeadThreshold / ent1DeadThreshold;
            if (!organDamages.TryAdd(category, modifiedDamage))
                organDamages[category] += modifiedDamage;
        }

        return organDamages;
    }

    /// <summary>
    /// Calculates the total damage from vital body parts (Head, Torso), for mobs with Body.
    /// For non-mobs, returns the total damage from the target entity.
    /// </summary>
    /// <returns>Total damage from vital body parts, or total damage if not a Body mob.</returns>
    public FixedPoint2 CheckVitalDamage(Entity<DamageableComponent?> ent)
    {
        if (!_damageQuery.Resolve(ent, ref ent.Comp, false))
            return FixedPoint2.Zero;

        if (!_bodyQuery.HasComp(ent))
            return _damageable.GetTotalDamage(ent);

        var result = FixedPoint2.Zero;
        foreach (var part in _body.GetVitalParts(ent))
        {
            result += _damageable.GetTotalDamage(part);
        }

        return result;
    }

    /// <summary>
    /// Transfers scaled damage from target1 to target2
    /// </summary>
    public void TransferDamage(EntityUid target1, EntityUid target2)
    {
        if (!_damageQuery.TryComp(target2, out var damageComp) ||
            !GetScaledDamage(target1, target2, out var damage, out var organDamages) ||
            damage == null)
            return;

        if (_bodyQuery.TryComp(target2, out var body))
        {
            var organs = _body.GetOrgans((target2, body));
            foreach (var organ in organs)
            {
                if (organ.Comp.Category is not { } category || organDamages == null || !organDamages.TryGetValue(category, out var organDamage))
                    continue;

                _damageable.SetDamage(organ.Owner, organDamage);
            }
        }

        _damageable.SetDamage((target2, damageComp), damage);
    }
}
