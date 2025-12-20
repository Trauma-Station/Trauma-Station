// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._Shitmed.Body;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

/// <summary>
/// Trauma - random internal and API additions to thresholds system.
/// </summary>
public sealed partial class MobThresholdSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private void InitializeTrauma()
    {
        SubscribeLocalEvent<MobThresholdsComponent, WoundableIntegrityChangedOnBodyEvent>(OnWoundableDamage);
    }

    private void OnWoundableDamage(Entity<MobThresholdsComponent> ent, ref WoundableIntegrityChangedOnBodyEvent args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobState))
            return;

        UpdateAlerts(ent, mobState.CurrentState, ent.Comp);
    }

    /// <summary>
    /// Version of GetScaledDamage that also gets the parts damage, indexed by targeting doll.
    /// </summary>
    public bool GetScaledDamage(
        EntityUid target1,
        EntityUid target2,
        out DamageSpecifier? damage,
        out Dictionary<TargetBodyPart, DamageSpecifier>? woundableDamage)
    {
        woundableDamage = null;
        if (!GetScaledDamage(target1, target2, out damage))
            return false;

        woundableDamage = GetScaledPartsDamage(target1, target2);
        return true;
    }

    private Dictionary<TargetBodyPart, DamageSpecifier>? GetScaledPartsDamage(EntityUid target1, EntityUid target2)
    {
        // If the receiver is a simplemob, we don't care about any of this. Just grab the damage and go.
        if (!TryComp<BodyComponent>(target2, out var body) || body.BodyType != BodyType.Complex)
            return null;

        // However if they are valid for woundmed, we first check if the sender is also valid for it to build a dict.
        if (!TryComp<BodyComponent>(target1, out var oldBody) ||
            oldBody.BodyType != BodyType.Complex ||
            !_body.TryGetRootPart(target1, out var parentRootPart))
            return null;

        if (!TryGetThresholdForState(target1, MobState.Dead, out var ent1DeadThreshold))
            ent1DeadThreshold = 0;

        if (!TryGetThresholdForState(target2, MobState.Dead, out var ent2DeadThreshold))
            ent2DeadThreshold = 0;

        Dictionary<TargetBodyPart, DamageSpecifier> entWoundablesDamage = new();
        foreach (var woundable in _wound.GetAllWoundableChildren(parentRootPart.Value))
        {
            if (woundable.Comp.WoundableIntegrity >= woundable.Comp.IntegrityCap
                || !TryComp<DamageableComponent>(parentRootPart.Value, out var damageable)
                || damageable.Damage.GetTotal() == 0)
                continue;

            var bodyPart = _body.GetTargetBodyPart(woundable);
            var modifiedDamage = damageable.Damage / ent1DeadThreshold.Value * ent2DeadThreshold.Value;
            if (!entWoundablesDamage.TryAdd(bodyPart, modifiedDamage))
                entWoundablesDamage[bodyPart] += modifiedDamage;
        }
        return entWoundablesDamage;
    }
}
