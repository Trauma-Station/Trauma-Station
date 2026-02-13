using Content.Medical.Common.Body;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// TargetBodyPart values that aren't a combination of others.
    /// Basically BodyPartType??
    /// </summary>
    public static readonly TargetBodyPart[] PrimitiveParts =
    [
        TargetBodyPart.Head,
        TargetBodyPart.Chest,
        TargetBodyPart.Groin,
        TargetBodyPart.LeftArm,
        TargetBodyPart.LeftHand,
        TargetBodyPart.RightArm,
        TargetBodyPart.RightHand,
        TargetBodyPart.LeftLeg,
        TargetBodyPart.LeftFoot,
        TargetBodyPart.RightLeg,
        TargetBodyPart.RightFoot
    ];

    /// <summary>
    /// Applies damage to an entity with body parts, targeting specific parts as needed.
    /// </summary>
    private DamageSpecifier ApplyDamageToBodyParts(
        EntityUid uid,
        DamageSpecifier damage,
        EntityUid? origin,
        bool ignoreResistances,
        bool interruptsDoAfters,
        TargetBodyPart? targetPart,
        float partMultiplier,
        bool ignoreBlockers = false,
        SplitDamageBehavior splitDamageBehavior = SplitDamageBehavior.Split,
        bool canMiss = true,
        bool increaseOnly = false)
    {
        // TODO SHITMED: jesus christ refactor this
        var adjustedDamage = damage * partMultiplier;
        // This cursed shitcode lets us know if the target part is a power of 2
        // therefore having multiple parts targeted.
        if (targetPart != null
            && targetPart != 0 && (targetPart & (targetPart - 1)) != 0)
        {
            // Extract only the body parts that are targeted in the bitmask
            var targetedBodyParts = new List<Entity<DamageableComponent>>();
            // Get only the primitive flags (powers of 2) - these are the actual individual body parts
            foreach (var flag in PrimitiveParts)
            {
                // Check if this specific flag is set in our targetPart bitmask
                if (!targetPart.Value.HasFlag(flag))
                    continue;

                // and that it exists on the target
                var (type, symmetry) = _body.ConvertTargetBodyPart(flag);
                foreach (var part in _part.GetBodyParts(uid, type, symmetry))
                {
                    if (_damageableQuery.TryComp(part, out var comp))
                        targetedBodyParts.Add((part, comp));
                }
            }

            // If we couldn't find any of the targeted parts, fall back to all body parts
            if (targetedBodyParts.Count == 0)
            {
                foreach (var part in _body.GetExternalOrgans(uid))
                {
                    if (_damageableQuery.TryComp(part, out var comp))
                        targetedBodyParts.Add((part, comp));
                }

                if (targetedBodyParts.Count == 0)
                    return new DamageSpecifier(); // erm no parts
            }

            List<float>? multipliers = null;
            var damagePerPart = adjustedDamage;
            if (adjustedDamage.PartDamageVariation != 0f)
            {
                multipliers =
                    GetDamageVariationMultipliers(uid, adjustedDamage.PartDamageVariation, targetedBodyParts.Count);
            }
            else
            {
                damagePerPart = ApplySplitDamageBehaviors(splitDamageBehavior, adjustedDamage, targetedBodyParts);
            }
            var appliedDamage = new DamageSpecifier();
            var surplusHealing = new DamageSpecifier();
            for (var i = 0; i < targetedBodyParts.Count; i++)
            {
                var (partId, partDamageable) = targetedBodyParts[i];
                var modifiedDamage = damagePerPart;
                if (multipliers != null && multipliers.Count == targetedBodyParts.Count)
                    modifiedDamage *= multipliers[i];
                modifiedDamage += surplusHealing;

                // Apply damage to this part
                var partDamageResult = ChangeDamage((partId, partDamageable), modifiedDamage, ignoreResistances,
                    interruptsDoAfters, origin, ignoreBlockers: ignoreBlockers);

                if (partDamageResult.Empty)
                    continue;

                appliedDamage += partDamageResult;

                /*
                    Why this ugly shitcode? Its so that we can track chems and other sorts of healing surpluses.
                    Assume you're fighting in a spaced area. Your chest has 30 damage, and every other part
                    is getting 0.5 per tick. Your chems will only be 10% as effective, so we take the surplus
                    healing and pass it along parts. That way a chem that would heal you for 75 brute would truly
                    heal the 75 brute per tick, and not some weird shit like 6.8 per tick.
                */
                foreach (var (type, damageFromDict) in modifiedDamage.DamageDict)
                {
                    if (damageFromDict >= 0
                        || !partDamageResult.DamageDict.TryGetValue(type, out var damageFromResult)
                        || damageFromResult > 0)
                        continue;

                    // If the damage from the dict plus the surplus healing is equal to the damage from the result,
                    // we can safely set the surplus healing to 0, as that means we consumed all of it.
                    surplusHealing.DamageDict[type] = (damageFromDict >= damageFromResult)
                        ? FixedPoint2.Zero
                        : damageFromDict - damageFromResult;
                }
            }

            return appliedDamage;
        }

        // Target a specific body part
        TargetBodyPart? target;
        var totalDamage = damage.GetTotal();

        if (totalDamage <= 0 || !canMiss) // Whoops i think i fucked up damage here.
            target = _body.GetTargetBodyPart(uid, origin, targetPart);
        else
            target = _body.GetRandomBodyPart(uid, origin, targetPart);

        var (partType, partSymmetry) = _body.ConvertTargetBodyPart(target);
        var possibleTargets = _part.GetBodyParts(uid, partType, partSymmetry);
        if (possibleTargets.Count == 0)
        {
            if (totalDamage <= 0)
                return new DamageSpecifier();

            foreach (var part in _body.GetExternalOrgans(uid))
            {
                possibleTargets.Add(part);
            }
        }

        // No body parts at all?
        if (possibleTargets.Count == 0)
            return new DamageSpecifier();

        // TODO: PredictedRandom when it's real
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(uid).Id);
        var rand = new System.Random(seed);
        var chosenTarget = rand.Pick(possibleTargets);
        return ChangeDamage(chosenTarget, adjustedDamage, ignoreResistances,
            interruptsDoAfters, origin, ignoreBlockers: ignoreBlockers, increaseOnly: increaseOnly);
    }

    private List<float> _weights = new();

    public List<float> GetDamageVariationMultipliers(EntityUid uid, float variation, int count)
    {
        DebugTools.AssertNotEqual(count, 0);
        variation = MathF.Abs(variation);
        var list = new List<float>(count);
        _weights.Clear();
        _weights.EnsureCapacity(count);
        var totalWeight = 0f;
        // TODO: proper predicted random
        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(uid).Id);
        var random = new System.Random(seed);
        for (var i = 0; i < count; i++)
        {
            var weight = random.NextFloat() * MathF.Abs(variation) + 1f;
            _weights.Add(weight);
            totalWeight += weight;
        }

        DebugTools.AssertNotEqual(totalWeight, 0f);

        foreach (var weight in _weights)
        {
            list.Add(weight / totalWeight);
        }

        return list;
    }

    /// <summary>
    /// Updates the parent entity's damage values by summing damage from all body parts.
    /// Should be called after damage is applied to any body part.
    /// </summary>
    /// <param name="body">The body it belongs to</param>
    /// <param name="appliedDamage">The damage that was applied to the body part</param>
    /// <param name="interruptsDoAfters">Whether this damage change interrupts do-afters</param>
    /// <param name="origin">The entity that caused the damage</param>
    /// <param name="ignoreBlockers">Whether to ignore damage blockers</param>
    /// <returns>True if parent damage was updated, false otherwise</returns>
    private bool UpdateParentDamageFromBodyParts(
        EntityUid body,
        DamageSpecifier? appliedDamage,
        bool interruptsDoAfters,
        EntityUid? origin,
        bool ignoreBlockers = false)
    {
        if (!_damageableQuery.TryComp(body, out var bodyDamage))
            return false;

        // Reset the parent's damage values
        foreach (var type in bodyDamage.Damage.DamageDict.Keys.ToList())
            bodyDamage.Damage.DamageDict[type] = FixedPoint2.Zero;
        Dirty(body, bodyDamage);

        // Sum up damage from all body parts
        foreach (var part in _body.GetOrgans<DamageableComponent>(body))
        {
            if (_internalQuery.HasComp(part)) // don't count internal organs, for now at least
                continue;

            foreach (var (type, value) in part.Comp.Damage.DamageDict)
            {
                if (value == 0)
                    continue;

                if (bodyDamage.Damage.DamageDict.TryGetValue(type, out var existing))
                    bodyDamage.Damage.DamageDict[type] = existing + value;
            }
        }

        // Raise the damage changed event on the parent
        OnEntityDamageChanged((body, bodyDamage),
            appliedDamage,
            interruptsDoAfters,
            origin,
            ignoreBlockers: ignoreBlockers);

        return true;
    }

    public DamageSpecifier ApplySplitDamageBehaviors(SplitDamageBehavior splitDamageBehavior,
        DamageSpecifier damage,
        List<Entity<DamageableComponent>> parts)
    {
        var newDamage = new DamageSpecifier(damage);
        switch (splitDamageBehavior)
        {
            case SplitDamageBehavior.None:
                return newDamage;
            case SplitDamageBehavior.Split:
                return newDamage / parts.Count;
            case SplitDamageBehavior.SplitEnsureAllDamaged:
                parts.RemoveAll(part => part.Comp.TotalDamage == FixedPoint2.Zero);

                goto case SplitDamageBehavior.SplitEnsureAll;
            case SplitDamageBehavior.SplitEnsureAllOrganic:
                parts.RemoveAll(part => _inorganicQuery.HasComp(part));

                goto case SplitDamageBehavior.SplitEnsureAll;
            case SplitDamageBehavior.SplitEnsureAllDamagedAndOrganic:
                parts.RemoveAll(part => part.Comp.TotalDamage == FixedPoint2.Zero);
                parts.RemoveAll(part => _inorganicQuery.HasComp(part));

                goto case SplitDamageBehavior.SplitEnsureAll;
            case SplitDamageBehavior.SplitEnsureAll:
                foreach (var (type, val) in newDamage.DamageDict)
                {
                    // project 0 comments :face_holding_back_tears:
                    if (val > 0)
                    {
                        if (parts.Count > 0)
                            newDamage.DamageDict[type] = val / parts.Count;
                        else
                            newDamage.DamageDict[type] = FixedPoint2.Zero;
                    }
                    else if (val < 0)
                    {
                        var count = 0;

                        foreach (var part in parts)
                        {
                            if (part.Comp.Damage.DamageDict.TryGetValue(type, out var currentDamage)
                                && currentDamage > 0)
                                count++;
                        }

                        if (count > 0)
                            newDamage.DamageDict[type] = val / count;
                        else
                            newDamage.DamageDict[type] = FixedPoint2.Zero;
                    }
                }
                // We sort the parts to ensure that surplus damage gets passed from least to most damaged.
                parts.Sort((a, b) => a.Comp.TotalDamage.CompareTo(b.Comp.TotalDamage));
                return newDamage;
            default:
                return damage;
        }
    }

    public void SetDamageContainerID(Entity<DamageableComponent?> ent, string damageContainerId)
    {
        if (!_damageableQuery.Resolve(ent, ref ent.Comp) || ent.Comp.DamageContainerID == damageContainerId)
            return;

        ent.Comp.DamageContainerID = damageContainerId;
        Dirty(ent);
    }
}
