// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mobs.Systems;

/// <summary>
/// Trauma - GetScaledDamage overload for polymorph transferring part damage
/// </summary>
public sealed partial class MobThresholdSystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly BodySystem _body = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<DamageableComponent> _damageQuery;

    private void InitializeTrauma()
    {
        _bodyQuery = GetEntityQuery<BodyComponent>();
        _damageQuery = GetEntityQuery<DamageableComponent>();
    }

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

    private Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier>? GetScaledPartsDamage(EntityUid target1, EntityUid target2)
    {
        // If the receiver is a simplemob, we don't care about any of this. Just grab the damage and go.
        if (!_bodyQuery.HasComp(target2))
            return null;

        // However if they are valid for woundmed, we first check if the sender is also valid for it to build a dict.
        if (!_bodyQuery.TryComp(target1, out var oldBody))
            return null;

        if (!TryGetThresholdForState(target1, MobState.Dead, out var ent1DeadThreshold))
            ent1DeadThreshold = 0;

        if (!TryGetThresholdForState(target2, MobState.Dead, out var ent2DeadThreshold))
            ent2DeadThreshold = 0;

        Dictionary<ProtoId<OrganCategoryPrototype>, DamageSpecifier> organDamages = new();
        foreach (var organ in _body.GetOrgans((target1, oldBody)))
        {
            if (organ.Comp.Category is not {} category
                || !_damageQuery.TryComp(organ, out var damageable)
                || damageable.Damage.GetTotal() == 0)
                continue;

            var modifiedDamage = damageable.Damage / ent1DeadThreshold.Value * ent2DeadThreshold.Value;
            if (!organDamages.TryAdd(category, modifiedDamage))
                organDamages[category] += modifiedDamage;
        }

        return organDamages;
    }
}
