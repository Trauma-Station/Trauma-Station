// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
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

    private EntityQuery<QualityComponent> _qualityQuery;

    private static readonly EntProtoId CraftingKnowledge = "CraftingKnowledge";

    private void InitializeQuality()
    {
        _qualityQuery = GetEntityQuery<QualityComponent>();

        SubscribeLocalEvent<QualityComponent, UpdateItemQualityEvent>(ConstructionInteraction);
        SubscribeLocalEvent<QualityComponent, GetMeleeDamageEvent>(AlterMeleeDamage);
        SubscribeLocalEvent<QualityComponent, RefreshNameModifiersEvent>(AlterName);
        SubscribeLocalEvent<QualityComponent, StackSplitEvent>(SplitStack);
        SubscribeLocalEvent<QualityComponent, AttemptMergeStackEvent>(AttemptMergeStack);
    }

    public void ConstructionInteraction(Entity<QualityComponent> ent, ref UpdateItemQualityEvent args)
    {
        var user = args.User;
        if (GetContainer(user) is not { } brain)
        {
            ModifyValues(ent);
            return;
        }

        int? lowestDelta = null;
        EntProtoId? lowestId = null;
        var knowledge = brain.Comp.KnowledgeDict;
        foreach (var (id, delta) in ent.Comp.LevelDeltas)
        {
            if (lowestDelta is not { } || (GetKnowledge(brain, id) is { } skill && GetMastery(skill.Comp) - delta < lowestDelta))
            {
                lowestDelta = delta;
                lowestId = id;
            }
        }

        int added = 0;
        if (GetKnowledge(brain, CraftingKnowledge) is { } crafting)
            added = crafting.Comp.Level + crafting.Comp.TemporaryLevel;
        else
            added = -1;

        var roll = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent)).Next(1, 100);


        ent.Comp.Quality = (added + ent.Comp.Quality + ent.Comp.QualityModifiers - roll) switch
        {
            >= 88 => 5,
            >= 44 => 4,
            >= 20 => 3,
            >= 10 => 2,
            >= 5 => 1,
            >= 0 => 0,
            >= -5 => -1,
            >= -10 => -2,
            >= -20 => -3,
            >= -44 => -4,
            _ => -5,
        };
        Dirty(ent);
        _nameModifier.RefreshNameModifiers(ent.Owner);
        ModifyValues(ent);

        var evCrafting = new AddExperienceEvent(CraftingKnowledge, Math.Abs(ent.Comp.Quality / 2));
        RaiseLocalEvent(user, ref evCrafting);

        if (lowestId is not { } actualId)
            return;

        var ev = new AddExperienceEvent(actualId, Math.Abs(ent.Comp.Quality / 2));
        RaiseLocalEvent(user, ref ev);
    }

    /// <summary>
    /// This should only ever be run once on any entity ever.
    /// </summary>
    public override void ModifyValues(Entity<QualityComponent> ent)
    {
        // TODO: make this dogshit an event
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
                    trigger.Damage *= ConstructionModifier(ent, ent.Comp.QualityCoefficent);
                }
            }
        }

        if (TryComp<DamageOtherOnHitComponent>(ent.Owner, out var thrown))
        {
            thrown.Damage *= ConstructionModifier(ent, ent.Comp.QualityCoefficent);
        }

        if (TryComp<GunComponent>(ent.Owner, out var gun))
        {
            gun.MaxAngle *= ConstructionModifier(ent, 1 / ent.Comp.QualityCoefficent);
        }

        if (TryComp<ProjectileComponent>(ent.Owner, out var projectile))
        {
            projectile.Damage *= ConstructionModifier(ent, ent.Comp.QualityCoefficent);
        }
    }

    public float ConstructionModifier(Entity<QualityComponent> ent, float power = 2)
        => MathF.Pow(power, ent.Comp.Quality);

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
        comp.QualityModifiers = ent.Comp.QualityModifiers;
        comp.QualityCoefficent = ent.Comp.QualityCoefficent;
        Dirty(args.NewId, comp);
        ModifyValues((args.NewId, comp));
    }

    private void AttemptMergeStack(Entity<QualityComponent> ent, ref AttemptMergeStackEvent args)
    {
        if (!_qualityQuery.TryComp(args.OtherStack, out var other))
        {
            args.Cancelled = true;
            return;
        }

        if (other.Quality != ent.Comp.Quality ||
            !LevelDeltasMatch(other.LevelDeltas, ent.Comp.LevelDeltas))
        {
            ent.Comp.QualityCoefficent = (ent.Comp.QualityCoefficent + other.QualityCoefficent) / 2;
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
