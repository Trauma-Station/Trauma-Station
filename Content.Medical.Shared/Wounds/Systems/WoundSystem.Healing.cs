// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Wounds;

/// <summary>
/// This class is responsible for managing wound healing in the shared game code.
/// It contains methods for halting all bleeding on a given entity.
/// </summary>
public partial class WoundSystem
{
    private List<Entity<WoundComponent>> _woundsToHeal = new(4);

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<WoundableComponent> ent, ref RejuvenateEvent args)
    {
        _container.CleanContainer(ent.Comp.Wounds); // no more wounds
    }

    #region Public API

    /// <summary>
    /// Heals bleeding wounds on a body entity, starting with the most severely bleeding woundable
    /// and cascading any leftover healing to the next most severe bleeding woundable.
    /// </summary>
    /// <param name="body">The body entity to check for bleeding wounds</param>
    /// <param name="healAmount">The amount of healing to apply</param>
    /// <param name="healed">The total amount of bleeding that was healed</param>
    /// <param name="component">Optional body component if already resolved</param>
    /// <returns>True if any bleeding was healed, false otherwise</returns>
    public bool TryHealMostSevereBleedingWoundables(EntityUid body, float healAmount, out FixedPoint2 healed, BodyComponent? component = null)
    {
        healed = FixedPoint2.Zero;
        if (!Resolve(body, ref component) || healAmount <= 0)
            return false;

        // Collect all woundables and their total bleeding amounts
        var bleedingWoundables = new List<(EntityUid Woundable, FixedPoint2 BleedAmount)>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var totalBleedAmount = FixedPoint2.Zero;
            var hasBleedingWounds = false;
            foreach (var wound in GetWoundableWounds(part.AsNullable()))
            {
                if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                    continue;

                hasBleedingWounds = true;
                totalBleedAmount += bleeds.BleedingAmount;
            }

            if (hasBleedingWounds)
                bleedingWoundables.Add((part.Owner, totalBleedAmount));
        }

        // Sort woundables by bleeding amount (descending)
        var sortedWoundables = bleedingWoundables
            .OrderByDescending(x => x.BleedAmount)
            .Select(x => x.Woundable)
            .ToList();

        float remainingHealAmount = healAmount * sortedWoundables.Count();
        bool anyHealed = false;

        // Apply healing to each woundable in order
        foreach (var woundable in sortedWoundables)
        {
            if (remainingHealAmount <= 0)
                break;

            FixedPoint2 modifiedBleed;
            bool didHeal = TryHealBleedingWounds(woundable, -remainingHealAmount, out modifiedBleed);
            if (!didHeal)
                continue;

            anyHealed = true;
            healed += -modifiedBleed - remainingHealAmount;
            remainingHealAmount -= (float) modifiedBleed;
        }

        return anyHealed;
    }

    public bool TryHealBleedingWounds(Entity<WoundableComponent?> part, FixedPoint2 bleedStopAbility, out FixedPoint2 modifiedBleed)
    {
        modifiedBleed = FixedPoint2.Zero;
        foreach (var wound in GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            DirtyField(wound, bleeds, nameof(BleedInflicterComponent.BleedingAmountRaw));

            if (bleedStopAbility <= bleeds.BleedingAmount)
            {
                bleeds.BleedingAmountRaw += bleedStopAbility;
                modifiedBleed += bleedStopAbility;
                break; // cant heal anymore
            }

            bleedStopAbility -= bleeds.BleedingAmount;
            modifiedBleed += bleeds.BleedingAmount;
            bleeds.BleedingAmountRaw = 0;
            bleeds.IsBleeding = false;
            bleeds.Scaling = 0;
            DirtyFields(wound, bleeds, null, nameof(BleedInflicterComponent.IsBleeding), nameof(BleedInflicterComponent.Scaling));
        }

        return modifiedBleed >= FixedPoint2.Zero;
    }

    public bool TryHealWounds(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        out FixedPoint2 healed,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        _woundsToHeal.Clear();
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (damageGroup != null && damageGroup != woundComp.DamageGroup ||
                !CanHealWound((wound, woundComp), ignoreBlockers))
                continue;

            _woundsToHeal.Add((wound, woundComp));
        }

        if (_woundsToHeal.Count == 0)
            return false;

        var heal = healAmount / _woundsToHeal.Count;
        foreach (var wound in _woundsToHeal)
        {
            healed += -ChangeWoundSeverity(wound, -heal);
        }

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    public bool TryHealWoundsOfType(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        [ForbidLiteral] ProtoId<DamageTypePrototype> damageType,
        out FixedPoint2 healed,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        _woundsToHeal.Clear();
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (damageType != woundComp.DamageType ||
                !CanHealWound((wound, woundComp), ignoreBlockers))
                continue;

            _woundsToHeal.Add((wound, woundComp));
        }

        if (_woundsToHeal.Count == 0)
            return false;

        var heal = -healAmount / _woundsToHeal.Count;
        foreach (var wound in _woundsToHeal)
        {
            healed += -ChangeWoundSeverity(wound, heal);
        }

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    public bool TryHealWounds(Entity<WoundableComponent?> part,
        DamageSpecifier damage,
        out Dictionary<string, FixedPoint2> healed,
        bool ignoreMultipliers = false)
    {
        healed = [];
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        foreach (var (type, amount) in damage.DamageDict)
        {
            if (TryHealWoundsOfType(part, amount, type, out var typeHealed))
            {
                healed.Add(type, typeHealed);
                continue;
            }
        }

        return healed.Any();
    }

    public bool TryGetWoundableWithMostDamage(
        EntityUid body,
        [NotNullWhen(true)] out Entity<WoundableComponent>? woundable,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool healable = false)
    {
        var biggestDamage = FixedPoint2.Zero;

        woundable = null;
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var woundableDamage = GetWoundableSeverityPoint(part.AsNullable(), damageGroup, healable);
            if (woundableDamage <= biggestDamage)
                continue;

            biggestDamage = woundableDamage;
            woundable = part;
        }

        return woundable != null;
    }

    public bool HasDamageOfGroup(
        EntityUid woundable,
        [ForbidLiteral] ProtoId<DamageGroupPrototype> damageGroup)
    {
        var wounds = GetWoundableWounds(woundable);
        return wounds.Any(wound => wound.Comp.DamageGroup == damageGroup);
    }

    public bool CanHealWound(Entity<WoundComponent> wound, bool ignoreBlockers = false)
    {
        if (!ignoreBlockers && !wound.Comp.CanBeHealed)
            return false;

        var holdingWoundable = wound.Comp.HoldingWoundable;

        var ev = new WoundHealAttemptOnWoundableEvent(wound);
        RaiseLocalEvent(holdingWoundable, ref ev);

        if (ev.Cancelled)
            return false;

        var ev1 = new WoundHealAttemptEvent((holdingWoundable, _woundableQuery.Comp(holdingWoundable)), ignoreBlockers);
        RaiseLocalEvent(wound, ref ev1);

        return !ev1.Cancelled;
    }

    /// <summary>
    /// Method to get all wounds of some entity
    /// The list is reused between calls with GetWoundableWounds, do not store it
    /// </summary>
    public bool TryGetBodyWounds(EntityUid body, out List<Entity<WoundComponent>> wounds)
    {
        wounds = _wounds;
        wounds.Clear();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            AddWounds(part, wounds);
        }

        return wounds.Count > 0;
    }

    private void AddWounds(WoundableComponent part, List<Entity<WoundComponent>> wounds)
    {
        foreach (var wound in part.Wounds.ContainedEntities)
        {
            if (_query.TryComp(wound, out var comp))
                wounds.Add((wound, comp));
        }
    }

    /// <summary>
    /// Method to get all wounded parts of entity
    /// </summary>
    public bool TryGetBodyWoundedParts(EntityUid body, out List<Entity<WoundableComponent>> woundables)
    {
        woundables = [];

        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (part.Comp.Wounds.Count > 0)
                woundables.Add(part);
        }

        return woundables.Count > 0;
    }

    /// <summary>
    /// Method to heal all wounds on entity by specific healing amount.
    /// </summary>
    public bool TryHealWoundsOnOwner(EntityUid body, DamageSpecifier healing, bool ignoreBlockers = false)
    {
        if (!TryGetBodyWoundedParts(body, out var woundables) || !TryGetBodyWounds(body, out var wounds))
            return false;

        DamageSpecifier healingPerPart = new DamageSpecifier(healing);
        healingPerPart.DamageDict.Clear();

        var woundCountByType = wounds
            .GroupBy(w => w.Comp.DamageType)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var healingType in healing.DamageDict)
        {
            var splitAmount = woundCountByType.GetValueOrDefault(healingType.Key, 0);

            // If we don't have wounds with our damage type just set it to heal value
            var splittedDamage = splitAmount != 0 ? healingType.Value / splitAmount : healingType.Value;

            healingPerPart.DamageDict.Add(healingType.Key, splittedDamage);
        }

        var healed = false;
        foreach (var woundable in woundables)
        {
            healed |= TryHealWounds(woundable.AsNullable(), healingPerPart, out _, ignoreBlockers);
        }

        return healed;
    }

    #endregion
}
