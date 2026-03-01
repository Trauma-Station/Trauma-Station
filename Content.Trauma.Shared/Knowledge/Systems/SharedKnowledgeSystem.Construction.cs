// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    private static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";

    private void InitializeConstruction()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, UpdateItemQualityEvent>(ConstructionInteraction);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, GetMeleeDamageEvent>(AlterMeleeDamage);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, RefreshNameModifiersEvent>(AlterName);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, InvokeArmorQualityEvent>(AlterArmorDamage);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, InvokeProjectileQualityEvent>(AlterProjectileDamage);
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, InvokeThrownQualityEvent>(AlterThrownDamage);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(DealShootingExperience);
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

    public void ConstructionInteraction(Entity<KnowledgeConstructionModifierComponent> ent, ref UpdateItemQualityEvent args)
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
            ent.Comp.Quality = qualityToAdd / ent.Comp.NumberOfMasteries;
            _nameModifier.RefreshNameModifiers(ent.Owner);
        }

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
    }

    public override float ConstructionModifier(Entity<KnowledgeConstructionModifierComponent> ent, float power = 2)
    {
        return (float) Math.Pow(power, ent.Comp.Quality);
    }

    private void AlterMeleeDamage(Entity<KnowledgeConstructionModifierComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= ConstructionModifier(ent);
    }

    private void AlterName(Entity<KnowledgeConstructionModifierComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier($"knowledge-modifier-name-{(int) Math.Clamp(ent.Comp.Quality, -5, 5)}");
    }

    private void AlterArmorDamage(Entity<KnowledgeConstructionModifierComponent> ent, ref InvokeArmorQualityEvent args)
    {
        args.Coefficient *= ConstructionModifier(ent, 0.87f);
    }

    private void AlterProjectileDamage(Entity<KnowledgeConstructionModifierComponent> ent, ref InvokeProjectileQualityEvent args)
    {
        args.Coefficient *= ConstructionModifier(ent, 1.75f);
    }

    private void AlterThrownDamage(Entity<KnowledgeConstructionModifierComponent> ent, ref InvokeThrownQualityEvent args)
    {
        args.Coefficient *= ConstructionModifier(ent, 1.75f);
    }

    private void DealShootingExperience(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter)
            return;

        var ev = new AddExperienceEvent(ShootingKnowledge, 1);
        RaiseLocalEvent(shooter, ref ev);
    }
}
