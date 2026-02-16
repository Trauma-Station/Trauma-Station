// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Medical.Shared.Consciousness;
using Content.Medical.Shared.Pain;
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
/// It contains methods for updating the pain state after wounds are healed,
/// and for halting all bleeding on a given entity.
/// </summary>
public partial class WoundSystem
{
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly PainSystem _pain = default!;

    private void InitializeHealing()
    {
        SubscribeLocalEvent<WoundableComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnRejuvenate(Entity<WoundableComponent> ent, ref RejuvenateEvent args)
    {
        _container.EmptyContainer(ent.Comp.Wounds); // no more wounds
    }

    // Updates pain state after wounds are healed and starts pain decay
    /// <param name="woundable">The entity on which to update the pain state</param>
    private void UpdatePainAfterHealing(EntityUid woundable)
    {
        // Check if the body has a brain feeling pain
        if (_body.GetBody(woundable) is not {} body ||
            // phantom pain award
            !_consciousness.TryGetNerveSystem(body, out var brain) ||
            brain.Value.Comp.Pain <= FixedPoint2.Zero)
            return;

        // Calculate decay duration based on current pain level - 12 seconds per pain point
        // 50 pain * 12 seconds per pain point = 600 seconds = 10 minutes
        var decayDuration = TimeSpan.FromSeconds(brain.Value.Comp.Pain.Float() * 12);

        // Start the pain decay process
        _pain.StartPainDecay(body, brain.Value.Comp.Pain, decayDuration, brain.Value.Comp);
    }

    #region Public API

    public bool TryHaltAllBleeding(EntityUid woundable, WoundableComponent? component = null, bool force = false)
    {
        if (!Resolve(woundable, ref component)
            || component.Wounds == null
            || component.Wounds.Count == 0)
            return true;

        foreach (var wound in GetWoundableWounds(woundable, component))
        {
            if (force)
            {
                // For wounds like scars. Temporary for now
                wound.Comp.CanBeHealed = true;
            }

            if (!TryComp<BleedInflicterComponent>(wound, out var bleeds))
                continue;

            bleeds.IsBleeding = false;
        }

        return true;
    }

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
            foreach (var wound in GetWoundableWounds(part, part.Comp))
            {
                if (!TryComp<BleedInflicterComponent>(wound, out var bleeds) || !bleeds.IsBleeding)
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

    public bool TryHealBleedingWounds(EntityUid woundable, float bleedStopAbility, out FixedPoint2 modifiedBleed, WoundableComponent? component = null)
    {
        modifiedBleed = FixedPoint2.Zero; // Goobstation
        if (!Resolve(woundable, ref component))
            return false;

        foreach (var wound in GetWoundableWounds(woundable, component))
        {
            if (!TryComp<BleedInflicterComponent>(wound, out var bleeds)
                || !bleeds.IsBleeding)
                continue;

            if (-bleedStopAbility > bleeds.BleedingAmount) // Goobstation
            {
                modifiedBleed = bleeds.BleedingAmount; // Goobstation
                bleeds.BleedingAmountRaw = 0;
                bleeds.IsBleeding = false;
                bleeds.Scaling = 0;
            }
            else
            {
                bleeds.BleedingAmountRaw += bleedStopAbility; // Goobstation
                modifiedBleed = -bleedStopAbility; // Goobstation
            }

            Dirty(wound, bleeds);
        }
        return modifiedBleed <= -bleedStopAbility; // Goobstation
    }

    public void ForceHealWoundsOnWoundable(EntityUid woundable,
        out FixedPoint2 healed,
        DamageGroupPrototype? damageGroup = null,
        WoundableComponent? component = null)
    {
        healed = 0;
        if (!Resolve(woundable, ref component))
            return;

        var woundsToHeal =
            GetWoundableWounds(woundable, component)
                .Where(wound => damageGroup == null || wound.Comp.DamageGroup == damageGroup)
                .ToList();

        foreach (var wound in woundsToHeal)
        {
            healed += wound.Comp.WoundSeverityPoint;
            RemoveWound(wound, wound);
        }

        UpdateWoundableIntegrity(woundable, component);
        CheckWoundableSeverityThresholds(woundable, component);

        // Update pain state after healing wounds if any wounds were healed
        if (woundsToHeal.Count > 0)
        {
            UpdatePainAfterHealing(woundable);
        }
    }

    public bool TryHealWoundsOnWoundable(EntityUid woundable,
        FixedPoint2 healAmount,
        out FixedPoint2 healed,
        WoundableComponent? component = null,
        DamageGroupPrototype? damageGroup = null,
        bool ignoreMultipliers = false,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!Resolve(woundable, ref component)
            || component.Wounds == null)
            return false;

        var woundsToHeal =
            (from wound in component.Wounds.ContainedEntities
                let woundComp = Comp<WoundComponent>(wound)
                where CanHealWound(wound, woundComp, ignoreBlockers)
                where damageGroup == null || damageGroup == woundComp.DamageGroup
                select (wound, woundComp)).Select(dummy => (Entity<WoundComponent>) dummy)
            .ToList(); // that's what I call LINQ. // kys mocho

        if (woundsToHeal.Count == 0)
            return false;

        var healNumba = healAmount / woundsToHeal.Count;
        var actualHeal = FixedPoint2.Zero;
        foreach (var wound in woundsToHeal)
        {
            var heal = ignoreMultipliers
                ? ApplyHealingRateMultipliers(wound, woundable, -healNumba, component)
                : -healNumba;

            actualHeal += -heal;
            ApplyWoundSeverity(wound, heal, wound);
        }

        UpdateWoundableIntegrity(woundable, component);
        CheckWoundableSeverityThresholds(woundable, component);

        healed = actualHeal;
        return actualHeal > 0;
    }

    public bool TryHealWoundsOnWoundable(EntityUid woundable,
        FixedPoint2 healAmount,
        string damageType,
        out FixedPoint2 healed,
        WoundableComponent? component = null,
        bool ignoreMultipliers = false,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!Resolve(woundable, ref component, false)
            || component.Wounds == null)
            return false;

        var woundsToHeal =
            (from wound in component.Wounds.ContainedEntities
                let woundComp = Comp<WoundComponent>(wound)
                where CanHealWound(wound, woundComp, ignoreBlockers)
                where damageType == woundComp.DamageType
                select (wound, woundComp)).Select(dummy => (Entity<WoundComponent>) dummy)
            .ToList(); // kys mocho

        if (woundsToHeal.Count == 0)
            return false;

        var healNumba = healAmount / woundsToHeal.Count;
        var actualHeal = FixedPoint2.Zero;
        foreach (var wound in woundsToHeal)
        {
            var heal = ignoreMultipliers
                ? ApplyHealingRateMultipliers(wound, woundable, -healNumba, component)
                : -healNumba;

            actualHeal += -heal;
            ApplyWoundSeverity(wound, heal, wound);
        }

        UpdateWoundableIntegrity(woundable, component);
        CheckWoundableSeverityThresholds(woundable, component);

        healed = actualHeal;
        return actualHeal > 0;
    }

    public bool TryHealWoundsOnWoundable(EntityUid woundable,
        DamageSpecifier damage,
        out Dictionary<string, FixedPoint2> healed,
        WoundableComponent? component = null,
        bool ignoreMultipliers = false)
    {
        healed = [];
        if (!Resolve(woundable, ref component, false))
            return false;

        foreach (var (key, value) in damage.DamageDict)
        {
            if (TryHealWoundsOnWoundable(woundable, -value, key, out var tempHealed, component, ignoreMultipliers))
            {
                healed.Add(key, tempHealed);
                continue;
            }
        }

        return healed.Any();
    }

    public bool TryGetWoundableWithMostDamage(
        EntityUid body,
        [NotNullWhen(true)] out Entity<WoundableComponent>? woundable,
        string? damageGroup = null,
        bool healable = false)
    {
        var biggestDamage = FixedPoint2.Zero;

        woundable = null;
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var woundableDamage = GetWoundableSeverityPoint(part, part.Comp, damageGroup, healable);
            if (woundableDamage <= biggestDamage)
                continue;

            biggestDamage = woundableDamage;
            woundable = part;
        }

        return woundable != null;
    }

    public bool HasDamageOfType(
        EntityUid woundable,
        string damageType)
    {
        // TODO: implement healable, this used to have 2 branches that returned the same thing
        var wounds = GetWoundableWounds(woundable);
        return wounds.Any(wound => wound.Comp.DamageType == damageType);
    }

    public bool HasDamageOfGroup(
        EntityUid woundable,
        string damageGroup)
    {
        var wounds = GetWoundableWounds(woundable);
        return wounds.Any(wound => wound.Comp.DamageGroup == damageGroup);
    }

    public FixedPoint2 ApplyHealingRateMultipliers(EntityUid wound,
        EntityUid woundable,
        FixedPoint2 severity,
        WoundableComponent? component = null,
        WoundComponent? woundComp = null)
    {
        if (!Resolve(woundable, ref component))
            return severity;

        if (!Resolve(wound, ref woundComp, false)
            || !woundComp.CanBeHealed)
            return FixedPoint2.Zero;

        var woundHealingMultiplier =
            _prototype.Index<DamageTypePrototype>(Comp<WoundComponent>(wound).DamageType).WoundHealingMultiplier;

        if (component.HealingMultipliers.Count == 0)
            return severity * woundHealingMultiplier;

        var toMultiply =
            component.HealingMultipliers.Sum(multiplier => (float) multiplier.Value.Change) / component.HealingMultipliers.Count;
        return severity * toMultiply * woundHealingMultiplier;
    }

    public bool TryAddHealingRateMultiplier(EntityUid owner, EntityUid woundable, string identifier, FixedPoint2 change, WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component) || !_net.IsServer)
            return false;

        return component.HealingMultipliers.TryAdd(owner, new WoundableHealingMultiplier(change, identifier));
    }

    public bool TryRemoveHealingRateMultiplier(EntityUid owner, EntityUid woundable, WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component)  || !_net.IsServer)
            return false;

        return component.HealingMultipliers.Remove(owner);
    }

    public bool CanHealWound(EntityUid wound, WoundComponent? comp = null, bool ignoreBlockers = false)
    {
        if (!Resolve(wound, ref comp))
            return false;

        if (!ignoreBlockers && !comp.CanBeHealed)
            return false;

        var holdingWoundable = comp.HoldingWoundable;

        var ev = new WoundHealAttemptOnWoundableEvent((wound, comp));
        RaiseLocalEvent(holdingWoundable, ref ev);

        if (ev.Cancelled)
            return false;

        var ev1 = new WoundHealAttemptEvent((holdingWoundable, Comp<WoundableComponent>(holdingWoundable)), ignoreBlockers);
        RaiseLocalEvent(wound, ref ev1);

        return !ev1.Cancelled;
    }

    /// <summary>
    /// Method to get all wounds of some entity
    /// </summary>
    /// <param name="target"></param>
    /// <param name="wounds"></param>
    /// <returns></returns>
    public bool TryGetAllOwnerWounds(EntityUid target, out List<Entity<WoundComponent>> wounds)
    {
        wounds = [];

        foreach (var part in _body.GetOrgans<WoundableComponent>(target))
        {
            GetWounds(part, wounds);
        }

        return wounds.Count > 0;
    }

    private void GetWounds(WoundableComponent woundable, List<Entity<WoundComponent>> wounds)
    {
        if (woundable.Wounds is not {} container)
            return;

        foreach (var wound in container.ContainedEntities)
        {
            if (_query.TryComp(wound, out var comp))
                wounds.Add((wound, comp));
        }
    }

    /// <summary>
    /// Method to get all wounded parts of entity
    /// </summary>
    /// <param name="target"></param>
    /// <param name="woundables"></param>
    /// <returns></returns>
    public bool TryGetAllOwnerWoundedParts(EntityUid target, out List<Entity<WoundableComponent>> woundables)
    {
        woundables = [];

        foreach (var part in _body.GetOrgans<WoundableComponent>(target))
        {
            if (part.Comp.Wounds.Count > 0)
                woundables.Add(part);
        }

        return woundables.Count > 0;
    }

    /// <summary>
    /// Method to heal all wounds on entity by specific healing amount.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="healing"></param>
    /// <param name="ignoreBlockers"></param>
    /// <returns></returns>
    public bool TryHealWoundsOnOwner(EntityUid target, DamageSpecifier healing, bool ignoreBlockers = false)
    {
        var healedWounds = 0;

        if (!TryGetAllOwnerWoundedParts(target, out var woundables) || !TryGetAllOwnerWounds(target, out var wounds))
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

        foreach (var woundable in woundables)
        {
            if (!TryHealWoundsOnWoundable(woundable.Owner, healingPerPart, out var healed, woundable.Comp, ignoreBlockers))
                continue;

            healedWounds++;
        }

        return healedWounds > 0;
    }

    #endregion
}
