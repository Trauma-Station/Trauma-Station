// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Stack;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private static readonly EntProtoId CraftingKnowledge = "CraftingKnowledge";

    private void InitializeQuality()
    {
        SubscribeLocalEvent<QualityComponent, UpdateItemQualityEvent>(ConstructionInteraction);
        SubscribeLocalEvent<QualityComponent, GetMeleeDamageEvent>(AlterMeleeDamage);
        SubscribeLocalEvent<QualityComponent, RefreshNameModifiersEvent>(AlterName);
        SubscribeLocalEvent<QualityComponent, StackSplitEvent>(SplitStack);
        SubscribeLocalEvent<QualityComponent, AttemptMergeStackEvent>(AttemptMergeStack);
    }

    public void ConstructionInteraction(Entity<QualityComponent> ent, ref UpdateItemQualityEvent args)
    {
        var user = args.User;

        if (TryGetKnowledgeDictionary(user) is { } userKnowledge)
        {
            int ownMasteries = 0;
            int itemMasteries = 0;
            foreach (var entity in ent.Comp.LevelDeltas)
            {
                var mastery = GetMastery(userKnowledge.GetValueOrDefault(entity.Key));
                ownMasteries += mastery;
                itemMasteries += entity.Value;
                var ev = new AddExperienceEvent(entity.Key, 6 - mastery);
                RaiseLocalEvent(user, ref ev);
            }
            int added = 0;
            if (TryGetKnowledgeUnit(user, CraftingKnowledge) is { } crafting)
                added = GetMastery(crafting) - 2;
            else
                added = -3;
            added = added + (ownMasteries - itemMasteries) / ent.Comp.LevelDeltas.Count();
            var evCrafting = new AddExperienceEvent(CraftingKnowledge, itemMasteries);
            RaiseLocalEvent(user, ref evCrafting);
            var qualityToAdd = ent.Comp.Quality * ent.Comp.NumberOfMasteries + added;
            ent.Comp.NumberOfMasteries++;
            ent.Comp.Quality = Math.Clamp(qualityToAdd / ent.Comp.NumberOfMasteries, -6, 6); // Make sure numbers don't go too crazy.
            _nameModifier.RefreshNameModifiers(ent.Owner);
        }
        ModifyValues(ent);
    }

    /// <summary>
    /// This should only ever be run once on any entity ever.
    /// </summary>
    /// <param name="ent"></param>
    public override void ModifyValues(Entity<QualityComponent> ent)
    {
        if (TryComp<ArmorComponent>(ent.Owner, out var armor))
        {
            var newModifiers = new DamageModifierSet
            {
                Coefficients = new(),
                FlatReduction = new Dictionary<string, float>(armor.Modifiers.FlatReduction),
                IgnoreArmorPierceFlags = armor.Modifiers.IgnoreArmorPierceFlags
            };
            var modifier = ConstructionModifier(ent, 0.87f);
            foreach (var modifiers in armor.Modifiers.Coefficients)
            {
                newModifiers.Coefficients.Add(modifiers.Key, modifiers.Value * modifier);
            }
            armor.Modifiers = newModifiers;
            Dirty(ent.Owner, armor);
        }

        if (TryComp<DestructibleComponent>(ent.Owner, out var destructible))
        {
            foreach (var threshold in destructible.Thresholds)
            {
                if (threshold.Trigger is DamageTrigger trigger)
                {
                    trigger.Damage *= ConstructionModifier(ent, 1.6f);
                }
            }
        }

        if (TryComp<DamageOtherOnHitComponent>(ent.Owner, out var thrown))
        {
            thrown.Damage *= ConstructionModifier(ent, 1.75f);
        }

        if (TryComp<GunComponent>(ent.Owner, out var gun))
        {
            gun.MaxAngle *= ConstructionModifier(ent, 0.9f);
        }

        if (TryComp<ProjectileComponent>(ent.Owner, out var projectile))
        {
            projectile.Damage *= ConstructionModifier(ent, 1.75f);
        }
    }

    public float ConstructionModifier(Entity<QualityComponent> ent, float power = 2)
    {
        return (float) Math.Pow(power, ent.Comp.Quality);
    }

    private void AlterMeleeDamage(Entity<QualityComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= ConstructionModifier(ent);
    }

    private void AlterName(Entity<QualityComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier($"quality-name-{(int) Math.Clamp(ent.Comp.Quality, -5, 5)}");
    }

    private void SplitStack(Entity<QualityComponent> ent, ref StackSplitEvent args)
    {
        var comp = EnsureComp<QualityComponent>(args.NewId);
        comp.LevelDeltas = ent.Comp.LevelDeltas;
        comp.Quality = ent.Comp.Quality;
        comp.NumberOfMasteries = ent.Comp.NumberOfMasteries;
        ModifyValues((args.NewId, comp));
    }

    private void AttemptMergeStack(Entity<QualityComponent> ent, ref AttemptMergeStackEvent args)
    {
        if (!TryComp<QualityComponent>(args.OtherStack, out var other))
        {
            args.Cancelled = true;
            return;
        }

        if (other.Quality != ent.Comp.Quality ||
        other.NumberOfMasteries != ent.Comp.NumberOfMasteries ||
        !LevelDeltasMatch(other.LevelDeltas, ent.Comp.LevelDeltas))
        {
            args.Cancelled = true;
        }
    }

    private bool LevelDeltasMatch(Dictionary<EntProtoId, int> a, Dictionary<EntProtoId, int> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var (key, value) in a)
        {
            if (!b.TryGetValue(key, out var otherValue) || value != otherValue)
                return false;
        }
        return true;
    }
}
