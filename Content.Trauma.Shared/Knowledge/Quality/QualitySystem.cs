// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.Explosion.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Construction;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Common.Quality;
using Content.Trauma.Common.Stack;
using Content.Trauma.Shared.Damage;
using Content.Trauma.Shared.Durability.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Quality;

/// <summary>
/// Handles quality interactions for construction, projectiles, etc.
/// </summary>
public sealed class QualitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly EntityQuery<QualityComponent> _query = default!;

    private static readonly EntProtoId FabricationKnowledge = "FabricationKnowledge";
    private static readonly ProtoId<KnowledgeCategoryPrototype> CraftingCategory = "Crafting";

    public override void Initialize()
    {
        base.Initialize();

        // quality effects
        SubscribeLocalEvent<QualityComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<QualityComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<ArmorComponent, ApplyQualityEvent>(OnArmorApplyQuality);
        SubscribeLocalEvent<ClothingComponent, ApplyQualityEvent>(OnClothingApplyQuality);
        SubscribeLocalEvent<ExplosionResistanceComponent, ApplyQualityEvent>(OnExplosionResistApplyQuality);
        SubscribeLocalEvent<StaminaResistanceComponent, ApplyQualityEvent>(OnStaminaResistApplyQuality);
        SubscribeLocalEvent<DestructibleComponent, ApplyQualityEvent>(OnDestructibleApplyQuality);
        SubscribeLocalEvent<DamageOnHitComponent, ApplyQualityEvent>(OnSelfDamageApplyQuality);
        SubscribeLocalEvent<DamageOtherOnHitComponent, ApplyQualityEvent>(OnDamageApplyQuality);
        SubscribeLocalEvent<MeleeWeaponComponent, ApplyQualityEvent>(OnMeleeDamageApplyQuality);
        SubscribeLocalEvent<GunComponent, ApplyQualityEvent>(OnGunApplyQuality);
        SubscribeLocalEvent<ProjectileComponent, ApplyQualityEvent>(OnProjectileApplyQuality);
        SubscribeLocalEvent<DurabilityComponent, ApplyQualityEvent>(OnDurabilityApplyQuality);
        SubscribeLocalEvent<BlockingComponent, ApplyQualityEvent>(OnShieldApplyQuality);

        // interactions
        SubscribeLocalEvent<QualityComponent, ConstructionChangedEvent>(OnConstructionChanged);
        SubscribeLocalEvent<QualityComponent, CartridgeFiredEvent>(OnCartridgeFired);
        SubscribeLocalEvent<QualityComponent, SpreadPelletFiredEvent>(OnSpreadPelletFired);
        SubscribeLocalEvent<QualityComponent, StackSplitEvent>(OnStackSplit);
        SubscribeLocalEvent<QualityComponent, AttemptMergeStackEvent>(OnAttemptMergeStack);
    }

    #region Quality effects

    private void OnRefreshNameModifiers(Entity<QualityComponent> ent, ref RefreshNameModifiersEvent args)
    {
        // TODO: quality should be clamped separately...
        var clamped = Math.Clamp(ent.Comp.Quality, -5, 5);
        args.AddModifier($"quality-name-{clamped}");
    }

    private void OnGunRefreshModifiers(Entity<QualityComponent> ent, ref GunRefreshModifiersEvent args)
    {
        // 60% spread at +5, 170% at -5
        var modifier = QualityModifier(_proto.Index(ent.Comp.QualityFactors).Gun);
        args.MinAngle *= modifier;
        args.MaxAngle *= modifier;
    }

    private void OnArmorApplyQuality(Entity<ArmorComponent> ent, ref ApplyQualityEvent args)
    {
        // -5 is half as good, 5 is twice as good
        var modifier = args.Modifier(args.Proto.Armor);
        var coefficients = ent.Comp.Modifiers.Coefficients;
        foreach (var damageType in coefficients.Keys)
        {
            coefficients[damageType] *= modifier;
        }
        Dirty(ent);
    }

    private void OnClothingApplyQuality(Entity<ClothingComponent> ent, ref ApplyQualityEvent args)
    {
        var modifier = args.Modifier(args.Proto.ClothingDelay);
        ent.Comp.EquipDelay *= modifier;
        Dirty(ent);
    }

    private void OnExplosionResistApplyQuality(Entity<ExplosionResistanceComponent> ent, ref ApplyQualityEvent args)
    {
        var modifier = args.Modifier(args.Proto.ExplosionResist);
        ent.Comp.DamageCoefficient *= modifier;
        Dirty(ent);
    }

    private void OnStaminaResistApplyQuality(Entity<StaminaResistanceComponent> ent, ref ApplyQualityEvent args)
    {
        var modifier = args.Modifier(args.Proto.StaminaResist);
        ent.Comp.DamageCoefficient *= modifier;
        Dirty(ent);
    }

    private void OnDestructibleApplyQuality(Entity<DestructibleComponent> ent, ref ApplyQualityEvent args)
    {
        var modifier = args.Modifier(args.Proto.Health);
        ent.Comp.Scale = modifier;
        Dirty(ent);
    }

    private void OnSelfDamageApplyQuality(Entity<DamageOnHitComponent> ent, ref ApplyQualityEvent args)
    {
        ent.Comp.Damage *= args.Modifier(args.Proto.SelfDamage);
        Dirty(ent);
    }

    // not specific to spears but holy class name
    private void OnDamageApplyQuality(Entity<DamageOtherOnHitComponent> ent, ref ApplyQualityEvent args)
    {
        // 180% damage at +5 quality
        ent.Comp.Damage *= args.Modifier(args.Proto.Damage);
        Dirty(ent);
    }

    private void OnMeleeDamageApplyQuality(Entity<MeleeWeaponComponent> ent, ref ApplyQualityEvent args)
    {
        var modifier = args.Modifier(args.Proto.MeleeDamage);
        foreach (var (id, value) in ent.Comp.Damage.DamageDict)
        {
            ent.Comp.Damage.DamageDict[id] = value * modifier;
        }
    }

    private void OnGunApplyQuality(Entity<GunComponent> ent, ref ApplyQualityEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
        // TODO: add gun jamming exploding in your face etc at low gun quality
    }

    private void OnProjectileApplyQuality(Entity<ProjectileComponent> ent, ref ApplyQualityEvent args)
    {
        ent.Comp.Damage *= args.Modifier(args.Proto.Projectile);
        Dirty(ent);
    }

    private void OnDurabilityApplyQuality(Entity<DurabilityComponent> ent, ref ApplyQualityEvent args)
    {
        ent.Comp.DamageProbability /= args.Modifier(args.Proto.Durability);
        Dirty(ent);
    }

    private void OnShieldApplyQuality(Entity<BlockingComponent> ent, ref ApplyQualityEvent args)
    {
        var modifierMinus = args.Modifier(args.Proto.Shield);
        var modifierPlus = args.Modifier(args.Proto.ShieldFlat);
        ent.Comp.PassiveBlockFraction *= modifierPlus;
        ent.Comp.ActiveBlockFraction *= modifierPlus;

        if (ent.Comp.PassiveBlockDamageModifer is { } passive)
        {
            foreach (var (key, number) in passive.Coefficients)
            {
                passive.Coefficients[key] = number * modifierMinus;
            }
            foreach (var (key, number) in passive.FlatReduction)
            {
                passive.FlatReduction[key] = number * modifierPlus;
            }
        }

        if (ent.Comp.ActiveBlockDamageModifier is { } active)
        {
            foreach (var (key, number) in active.Coefficients)
            {
                active.Coefficients[key] = number * modifierMinus;
            }
            foreach (var (key, number) in active.FlatReduction)
            {
                active.FlatReduction[key] = number * modifierPlus;
            }
        }
        Dirty(ent);
    }

    #endregion

    #region Interactions

    private void OnConstructionChanged(Entity<QualityComponent> ent, ref ConstructionChangedEvent args)
    {
        CopyQuality(ent, args.Target);
    }

    private void OnCartridgeFired(Entity<QualityComponent> ent, ref CartridgeFiredEvent args)
    {
        CopyQuality(ent, args.Bullet);
    }

    private void OnSpreadPelletFired(Entity<QualityComponent> ent, ref SpreadPelletFiredEvent args)
    {
        CopyQuality(ent, args.Pellet);
    }

    private void OnStackSplit(Entity<QualityComponent> ent, ref StackSplitEvent args)
    {
        var comp = EnsureComp<QualityComponent>(args.NewId);
        comp.LevelDeltas = ent.Comp.LevelDeltas;
        comp.Quality = ent.Comp.Quality;
        comp.QualityModifiers = ent.Comp.QualityModifiers;
        comp.QualityFactors = ent.Comp.QualityFactors;
        Dirty(args.NewId, comp);
        ApplyQuality((args.NewId, comp));
    }

    private void OnAttemptMergeStack(Entity<QualityComponent> ent, ref AttemptMergeStackEvent args)
    {
        if (!_query.TryComp(args.OtherStack, out var other))
        {
            args.Cancelled = true;
            return;
        }

        if (other.Quality != ent.Comp.Quality ||
            other.QualityModifiers != ent.Comp.QualityModifiers ||
            !LevelDeltasMatch(other.LevelDeltas, ent.Comp.LevelDeltas))
        {
            args.Cancelled = true;
        }
    }

    #endregion

    #region Helpers

    public void CopyQuality(Entity<QualityComponent> original, EntityUid created)
    {
        if (EnsureComp<QualityComponent>(created, out var newComp))
        {
            newComp.QualityModifiers += original.Comp.Quality * 5;
            Dirty(created, newComp);
            return;
        }

        newComp.LevelDeltas = original.Comp.LevelDeltas;
        newComp.Quality = original.Comp.Quality;
        newComp.QualityModifiers = original.Comp.QualityModifiers;
        newComp.QualityFactors = original.Comp.QualityFactors;
        Dirty(created, newComp);

        ApplyQuality((created, newComp));
    }

    /// <summary>
    /// This should only ever be run once on any entity ever.
    /// </summary>
    private void ApplyQuality(Entity<QualityComponent> ent)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);

        if (!_proto.Resolve(ent.Comp.QualityFactors, out var proto))
            return;

        var ev = new ApplyQualityEvent(ent.Comp.Quality, proto);
        RaiseLocalEvent(ent, ref ev);
    }

    // technically its not actually rolling but whatever
    public void RollQuality(Entity<QualityComponent> ent, EntityUid user)
    {
        if (_knowledge.GetContainer(user) is not { } brain)
        {
            ApplyQuality(ent);
            return;
        }

        var (knowledgeToUse, lowestId, lowestDelta, skillDelta) = FindLowestDelta(brain, ent.Comp.LevelDeltas);

        var added = _knowledge.GetKnowledge(brain, knowledgeToUse)?.Comp.NetLevel ?? -1;

        var roll = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent)).Next(1, 100);

        ent.Comp.Quality = (added + lowestDelta * 15 + ent.Comp.Quality + ent.Comp.QualityModifiers - roll) switch
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
        ApplyQuality(ent);

        // TODO: limit skill gain based on the recipe used
        _knowledge.AddExperience(brain, knowledgeToUse, Math.Abs(ent.Comp.Quality / 2) + 3, _knowledge.GetInverseMastery(skillDelta + 2));

        if (lowestId is not { } actualId)
            return;

        // TODO: above
        _knowledge.AddExperience(brain, actualId, Math.Abs(ent.Comp.Quality / 2) + 3, _knowledge.GetInverseMastery(skillDelta + 2));
    }

    public (EntProtoId, EntProtoId?, int, int) FindLowestDelta(Entity<KnowledgeContainerComponent> brain, Dictionary<EntProtoId, int> levelDeltas)
    {
        int lowestDelta = 0;
        int skillDelta = 0;
        EntProtoId? lowestId = null;
        EntProtoId knowledgeToUse = FabricationKnowledge;
        bool setKnowledge = false;
        foreach (var (id, delta) in levelDeltas)
        {
            if (_knowledge.GetKnowledge(brain, id) is not { } skill)
                continue;

            if (skill.Comp.Category == CraftingCategory && !setKnowledge)
            {
                knowledgeToUse = id;
                setKnowledge = true;
            }

            int smallestDelta = _knowledge.GetMastery(skill.Comp) - delta;
            if ((lowestId is not { } || smallestDelta < lowestDelta) && knowledgeToUse != id)
            {
                lowestDelta = _knowledge.GetMastery(skill.Comp) - delta;
                lowestId = id;
                skillDelta = delta;
            }
        }

        return (knowledgeToUse, lowestId, lowestDelta, skillDelta);
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

    // default is ~40% worse at -5, ~60% better at +5, not too crazy for most things
    public static float QualityModifier(float quality, float power = 1.1f)
        => MathF.Pow(power, quality);

    #endregion
}

/// <summary>
/// Raised on an entity to apply quality modifiers for each relevant component.
/// </summary>
[ByRefEvent]
public record struct ApplyQualityEvent(int Quality, QualityPrototype Proto)
{
    public float Modifier(float power = 1.1f)
        => QualitySystem.QualityModifier((float) Quality, power);
}
