// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityConditions;

/// <summary>
/// Checking for at least this amount of damage, but only for specified types/groups
/// If we have less, this condition is false.
/// </summary>
/// <remarks>
/// DamageSpecifier splits damage groups across types, we greedily revert that split to create
/// behaviour closer to what user expects; any damage in specified group contributes to that
/// group total. Use multiple conditions if you want to explicitly avoid that behaviour,
/// or don't use damage types within a group when specifying prototypes.
/// </remarks>
public sealed partial class TypedDamageCondition : EntityConditionBase<TypedDamageCondition>
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var damages = new List<string>();
        var comparison = new DamageSpecifier(Damage);
        foreach (var group in prototype.EnumeratePrototypes<DamageGroupPrototype>())
        {
            var lowestDamage = FixedPoint2.MaxValue;
            foreach (var damageType in group.DamageTypes)
            {
                if (comparison.DamageDict.TryGetValue(damageType, out var value))
                    lowestDamage = value < lowestDamage ? value : lowestDamage;
                else
                {
                    lowestDamage = FixedPoint2.Zero;
                    break;
                }
            }
            if (lowestDamage == FixedPoint2.MaxValue || lowestDamage == FixedPoint2.Zero)
                continue;
            var groupDamage = lowestDamage * group.DamageTypes.Count;
            if (MathF.Abs(groupDamage.Float() - MathF.Round(groupDamage.Float())) < 0.02)
                groupDamage = MathF.Round(groupDamage.Float());
            if (groupDamage > 0)
                damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", group.LocalizedName),
                    ("amount", MathF.Abs(groupDamage.Float())),
                    ("deltasign", 1))
                );
            foreach (var damageType in group.DamageTypes)
            {
                comparison.DamageDict[damageType] -= lowestDamage;
                if (MathF.Abs(comparison.DamageDict[damageType].Float()
                        - MathF.Round(comparison.DamageDict[damageType].Float()))
                        < 0.02)
                    comparison.DamageDict[damageType] = MathF.Round(comparison.DamageDict[damageType].Float());
            }
            comparison.ClampMin(0);
            comparison.TrimZeros();
        }

        foreach (var (kind, amount) in comparison.DamageDict)
        {
            damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                    ("amount", MathF.Abs(amount.Float())),
                    ("deltasign", 1))
                );
        }

        return Loc.GetString("reagent-effect-condition-guidebook-typed-damage-threshold",
            ("changes", ContentLocalizationManager.FormatList(damages)));
    }
}

public sealed class TypedDamageConditionSystem : EntityConditionSystem<DamageableComponent, TypedDamageCondition>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Condition(Entity<DamageableComponent> ent, ref EntityConditionEvent<TypedDamageCondition> args)
    {
        var comparison = new DamageSpecifier(args.Condition.Damage);
        foreach (var group in _proto.EnumeratePrototypes<DamageGroupPrototype>())
        {
            // Greedily revert the split and check; Quickly skip when not relevant
            var lowestDamage = FixedPoint2.MaxValue;
            foreach (var damageType in group.DamageTypes)
            {
                if (!comparison.DamageDict.TryGetValue(damageType, out var value))
                {
                    lowestDamage = FixedPoint2.Zero;
                    break;
                }

                lowestDamage = value < lowestDamage ? value : lowestDamage;
            }

            if (lowestDamage == FixedPoint2.MaxValue || lowestDamage == FixedPoint2.Zero)
                continue;

            var groupDamage = lowestDamage * group.DamageTypes.Count;
            if (MathF.Abs(groupDamage.Float() - MathF.Round(groupDamage.Float())) < 0.02)
                groupDamage = MathF.Round(groupDamage.Float()); // otherwise brutes split unevenly
            if (ent.Comp.Damage.TryGetDamageInGroup(group, out var total) && total > groupDamage)
            {
                args.Result = true;
                return;
            }

            // we finished comparing this group, remove future interferences
            foreach (var damageType in group.DamageTypes)
            {
                comparison.DamageDict[damageType] -= lowestDamage;
                // not a fan, but it's needed
                if (MathF.Abs(comparison.DamageDict[damageType].Float()
                    - MathF.Round(comparison.DamageDict[damageType].Float()))
                    < 0.02)
                    comparison.DamageDict[damageType] = MathF.Round(comparison.DamageDict[damageType].Float());
            }
            comparison.ClampMin(0);
            comparison.TrimZeros();
        }

        comparison.ExclusiveAdd(-ent.Comp.Damage);
        comparison = -comparison;
        args.Result = comparison.AnyPositive();
    }
}
