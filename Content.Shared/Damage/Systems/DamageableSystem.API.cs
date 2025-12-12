// <Trauma>
using Content.Shared._Shitmed.Body;
using Content.Shared._Shitmed.Body.Part;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body.Part;
// </Trauma>
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    /// <summary>
    ///     Directly sets the damage in a damageable component.
    ///     This method keeps the damage types supported by the DamageContainerPrototype in the component.
    ///     If a type is given in <paramref name="damage"/>, but not supported then it will not be set.
    ///     If a type is supported but not given in <paramref name="damage"/> then it will be set to 0.
    /// </summary>
    /// <remarks>
    ///     Useful for some unfriendly folk. Also ensures that cached values are updated and that a damage changed
    ///     event is raised.
    /// </remarks>
    public void SetDamage(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            if (damage.DamageDict.TryGetValue(type, out var value))
                ent.Comp.Damage.DamageDict[type] = value;
            else
                ent.Comp.Damage.DamageDict[type] = 0;
        }

        OnEntityDamageChanged((ent, ent.Comp));
    }

    /// <summary>
    ///     Directly sets the damage specifier of a damageable component.
    ///     This will overwrite the complete damage dict, meaning it will bulldoze the supported damage types.
    /// </summary>
    /// <remarks>
    ///     This may break persistance as the supported types are reset in case the component is initialized again.
    ///     So this only makes sense if you also change the DamageContainerPrototype in the component at the same time.
    ///     Only use this method if you know what you are doing.
    /// </remarks>
    public void SetDamageSpecifier(Entity<DamageableComponent?> ent, DamageSpecifier damage)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Damage = damage;

        OnEntityDamageChanged((ent, ent.Comp));
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        // <Shitmed>
        bool canBeCancelled = false,
        float partMultiplier = 1.00f,
        TargetBodyPart? targetPart = null,
        bool ignoreBlockers = false,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = true
        // </Shitmed>
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        return TryChangeDamage(ent, damage, out _, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers,
            canBeCancelled, partMultiplier, targetPart, ignoreBlockers, splitDamage, canMiss); // Shitmed
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     If the attempt was successful or not.
    /// </returns>
    public bool TryChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        out DamageSpecifier newDamage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        // <Shitmed>
        bool canBeCancelled = false,
        float partMultiplier = 1.00f,
        TargetBodyPart? targetPart = null,
        bool ignoreBlockers = false,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = true
        // </Shitmed>
    )
    {
        //! Empty just checks if the DamageSpecifier is _literally_ empty, as in, is internal dictionary of damage types is empty.
        // If you deal 0.0 of some damage type, Empty will be false!
        newDamage = ChangeDamage(ent, damage, ignoreResistances, interruptsDoAfters, origin, ignoreGlobalModifiers,
            canBeCancelled, partMultiplier, targetPart, ignoreBlockers, splitDamage, canMiss); // Shitmed
        return !damage.Empty;
    }

    /// <summary>
    ///     Applies damage specified via a <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     <see cref="DamageSpecifier"/> is effectively just a dictionary of damage types and damage values. This
    ///     function just applies the container's resistances (unless otherwise specified) and then changes the
    ///     stored damage data. Division of group damage into types is managed by <see cref="DamageSpecifier"/>.
    /// </remarks>
    /// <returns>
    ///     The actual amount of damage taken, as a DamageSpecifier.
    /// </returns>
    public DamageSpecifier ChangeDamage(
        Entity<DamageableComponent?> ent,
        DamageSpecifier damage,
        bool ignoreResistances = false,
        bool interruptsDoAfters = true,
        EntityUid? origin = null,
        bool ignoreGlobalModifiers = false,
        // <Shitmed>
        bool canBeCancelled = false,
        float partMultiplier = 1.00f,
        TargetBodyPart? targetPart = null,
        bool ignoreBlockers = false,
        SplitDamageBehavior splitDamage = SplitDamageBehavior.Split,
        bool canMiss = true
        // </Shitmed>
    )
    {
        var damageDone = new DamageSpecifier();

        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return damageDone;

        if (damage.Empty)
            return damageDone;

        var before = new BeforeDamageChangedEvent(damage, origin,
            false, canBeCancelled, targetPart); // Shitmed
        RaiseLocalEvent(ent, ref before);

        if (before.Cancelled)
            return damageDone;

        // <Shitmed> - For entities with a body, route damage through body parts and then sum it up
        if (_bodyQuery.TryComp(ent, out var body) && body.BodyType == BodyType.Complex)
        {
            return ApplyDamageToBodyParts(ent, damage, origin, ignoreResistances,
                interruptsDoAfters, targetPart, partMultiplier, ignoreBlockers, splitDamage, canMiss);
        }

        // </Shitmed>

        // Apply resistances
        if (!ignoreResistances)
        {
            if (
                ent.Comp.DamageModifierSetId != null &&
                _prototypeManager.Resolve(ent.Comp.DamageModifierSetId, out var modifierSet)
            )
                damage = DamageSpecifier.ApplyModifierSet(damage,
                    DamageSpecifier.PenetrateArmor(modifierSet, damage.ArmorPenetration)); // Goob edit

            // <Shitmed>
            if (TryComp<BodyPartComponent>(ent, out var bodyPart))
            {
                TargetBodyPart? target = _body.GetTargetBodyPart(bodyPart);
                if (bodyPart.Body != null)
                {
                    // First raise the event on the parent to apply any parent modifiers
                    var parentEv = new DamageModifyEvent(bodyPart.Body.Value, damage, origin, target);
                    RaiseLocalEvent(bodyPart.Body.Value, parentEv);
                    damage = parentEv.Damage;
                }

                // Then raise on the part itself for any part-specific modifiers
                var ev = new DamageModifyEvent(ent, damage, origin, target);
                RaiseLocalEvent(ent, ev);
                damage = ev.Damage;
            }
            else
            {
                // Not a body part, just apply modifiers normally
                var ev = new DamageModifyEvent(ent, damage, origin);
                RaiseLocalEvent(ent, ev);
                damage = ev.Damage;
            }
            // </Shitmed>

            if (damage.Empty)
                return damageDone;
        }

        if (!ignoreGlobalModifiers)
            damage = ApplyUniversalAllModifiers(damage);

        // <Goob> - keep penetration etc values
        damageDone.ArmorPenetration = damage.ArmorPenetration;
        damageDone.PartDamageVariation = damage.PartDamageVariation;
        damageDone.WoundSeverityMultipliers = new(damage.WoundSeverityMultipliers);
        // </Goob>

        damageDone.DamageDict.EnsureCapacity(damage.DamageDict.Count);

        // <Shitmed> - Check for integrity cap on body parts
        bool isWoundable = false;
        FixedPoint2? damageCap = null;
        FixedPoint2? remainingCap = null;
        if (_woundableQuery.TryComp(ent, out var woundable))
        {
            isWoundable = true;
            damageCap = woundable.IntegrityCap;
            remainingCap = woundable.IntegrityCap - ent.Comp.TotalDamage;
        }
        // </Shitmed>

        var dict = ent.Comp.Damage.DamageDict;
        foreach (var (type, value) in damage.DamageDict)
        {
            // CollectionsMarshal my beloved.
            if (!dict.TryGetValue(type, out var oldValue))
                continue;

            // <Shitmed> - damage cap
            // For positive damage, we need to check if we've hit the cap
            if (value > 0)
            {
                // Delta ignores this stuff since we need it for effects.
                damageDone.DamageDict[type] = value;

                // If we're not a woundable or we don't have a cap, apply the damage normally
                if (!isWoundable
                    || remainingCap is null)
                {
                    dict[type] = oldValue + value;
                    continue;
                }

                // If we've already hit the cap, skip this damage type
                if (remainingCap.Value <= 0)
                    continue;

                // Calculate how much of this damage type we can apply
                var damageToApply = FixedPoint2.Min(value, remainingCap.Value);
                var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + damageToApply);

                // Update remaining cap
                remainingCap -= damageToApply;

                // Only update the dict if the value actually changed
                if (newValue != oldValue)
                    dict[type] = newValue;
            }
            else
            {
                // For negative damage (healing), apply normally
                var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
                if (newValue != oldValue)
                {
                    dict[type] = newValue;
                    damageDone.DamageDict[type] = newValue - oldValue;
                }
            }
            // </Shitmed>
        }

        // <Shitmed> - add ignoreGlobalModifiers, check woundable
        if (damageDone.Empty)
            return damageDone;

        OnEntityDamageChanged((ent, ent.Comp), damageDone, interruptsDoAfters, origin, ignoreGlobalModifiers);
        if (isWoundable)
        {
            // This means that the damaged part was a woundable
            // which also means we send that shit to refresh the body.
            UpdateParentDamageFromBodyParts(ent.Owner,
                damageDone,
                interruptsDoAfters,
                origin,
                ignoreBlockers: ignoreBlockers);
        }
        // </Shitmed>

        return damageDone;
    }

    /// <summary>
    /// Applies the two universal "All" modifiers, if set.
    /// Individual damage source modifiers are set in their respective code.
    /// </summary>
    /// <param name="damage">The damage to be changed.</param>
    public DamageSpecifier ApplyUniversalAllModifiers(DamageSpecifier damage)
    {
        // Checks for changes first since they're unlikely in normal play.
        if (
            MathHelper.CloseToPercent(UniversalAllDamageModifier, 1f) &&
            MathHelper.CloseToPercent(UniversalAllHealModifier, 1f)
        )
            return damage;

        foreach (var (key, value) in damage.DamageDict)
        {
            if (value == 0)
                continue;

            if (value > 0)
            {
                damage.DamageDict[key] *= UniversalAllDamageModifier;

                continue;
            }

            if (value < 0)
                damage.DamageDict[key] *= UniversalAllHealModifier;
        }

        return damage;
    }

    public void ClearAllDamage(Entity<DamageableComponent?> ent)
    {
        SetAllDamage(ent, FixedPoint2.Zero);
    }

    /// <summary>
    ///     Sets all damage types supported by a <see cref="Components.DamageableComponent"/> to the specified value.
    /// </summary>
    /// <remarks>
    ///     Does nothing If the given damage value is negative.
    /// </remarks>
    public void SetAllDamage(Entity<DamageableComponent?> ent, FixedPoint2 newValue)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        if (newValue < 0)
            return;

        // <Shitmed> - If entity has a body, set damage on all body parts
        if (_bodyQuery.HasComp(ent))
        {
            foreach (var (part, _) in _body.GetBodyChildren(ent.Owner))
            {
                if (!_damageableQuery.TryComp(part, out var partDamageable))
                    continue;

                // I LOVE RECURSION!!!
                SetAllDamage((part, partDamageable), newValue);
            }
        }
        // </Shitmed>

        foreach (var type in ent.Comp.Damage.DamageDict.Keys)
        {
            ent.Comp.Damage.DamageDict[type] = newValue;
        }
        ent.Comp.LastModifiedTime = _timing.CurTime; // Shitmed

        // Setting damage does not count as 'dealing' damage, even if it is set to a larger value, so we pass an
        // empty damage delta.
        OnEntityDamageChanged((ent, ent.Comp), new DamageSpecifier());

        // <Shitmed>
        if (_woundableQuery.TryComp(ent, out var woundable))
        {
            if (!woundable.AllowWounds) return;

            _wounds.UpdateWoundableIntegrity(ent, woundable);

            foreach (var (type, value) in ent.Comp.Damage.DamageDict)
            {
                var mul = ent.Comp.Damage.WoundSeverityMultipliers.GetValueOrDefault(type, 1);
                _wounds.TryInduceWound(ent, type, value * mul, out _, woundable);
            }
        }
        // </Shitmed>
    }

    /// <summary>
    /// Set's the damage modifier set prototype for this entity.
    /// </summary>
    /// <param name="ent">The entity we're setting the modifier set of.</param>
    /// <param name="damageModifierSetId">The prototype we're setting.</param>
    public void SetDamageModifierSetId(Entity<DamageableComponent?> ent, ProtoId<DamageModifierSetPrototype>? damageModifierSetId)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.DamageModifierSetId = damageModifierSetId;
        // <Goob>
        foreach (var (id, part) in _body.GetBodyChildren(ent.Owner))
        {
            EnsureComp<DamageableComponent>(id).DamageModifierSetId = damageModifierSetId;
        }
        // </Goob>

        Dirty(ent);
    }
}
