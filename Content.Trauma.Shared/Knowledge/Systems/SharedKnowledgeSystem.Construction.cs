// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;
using Content.Trauma.Common.Stack;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";

    private void InitializeConstruction()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);
        SubscribeLocalEvent<QualityComponent, UpdateItemQualityEvent>(ConstructionInteraction);
        SubscribeLocalEvent<QualityComponent, GetMeleeDamageEvent>(AlterMeleeDamage);
        SubscribeLocalEvent<QualityComponent, RefreshNameModifiersEvent>(AlterName);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(DealShootingExperience);
        SubscribeLocalEvent<QualityComponent, StackSplitEvent>(SplitStack);
        SubscribeLocalEvent<QualityComponent, AttemptMergeStackEvent>(AttemptMergeStack);
    }

    public void OnConstructionGetGroupEvent(Entity<KnowledgeHolderComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        if (TryGetAllKnowledgeUnits(ent) is not { } knowledge)
            return;

        foreach (var entity in knowledge)
        {
            if (Prototype(entity)?.ID is { } protoId && TryComp<KnowledgeComponent>(entity, out var comp))
                args.Groups.Add(protoId, comp.Level);
        }
    }

    public void ConstructionInteraction(Entity<QualityComponent> ent, ref UpdateItemQualityEvent args)
    {
        var user = args.User;

        if (TryGetKnowledgeDictionary(user) is { } userKnowledge)
        {
            int added = 0;
            foreach (var entity in ent.Comp.LevelDeltas)
            {
                var mastery = GetMastery(userKnowledge.GetValueOrDefault(entity.Key));
                added += mastery - entity.Value;
                var ev = new AddExperienceEvent(entity.Key, 6 - mastery);
                RaiseLocalEvent(user, ref ev);
            }
            added = added / ent.Comp.LevelDeltas.Count();
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
    public void ModifyValues(Entity<QualityComponent> ent)
    {
        if (TryComp<ArmorComponent>(ent.Owner, out var armor) && armor.Modifiers.Coefficients is { } armorModifiers)
        {
            foreach (var modifier in armorModifiers)
            {
                armorModifiers[modifier.Key] = ConstructionModifier(ent, 0.87f) * modifier.Value;
            }
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
        args.AddModifier($"knowledge-modifier-name-{(int) Math.Clamp(ent.Comp.Quality, -5, 5)}");
    }

    private void DealShootingExperience(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter)
            return;

        var ev = new AddExperienceEvent(ShootingKnowledge, 1);
        RaiseLocalEvent(shooter, ref ev);
    }

    private void SplitStack(Entity<QualityComponent> ent, ref StackSplitEvent args)
    {
        var comp = EnsureComp<QualityComponent>(args.NewId);
        comp.LevelDeltas = ent.Comp.LevelDeltas;
        comp.Quality = ent.Comp.Quality;
        comp.NumberOfMasteries = ent.Comp.NumberOfMasteries;
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
