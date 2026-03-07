// SPDX-License-Identifier: AGPL-3.0-or-later

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
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Construction;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Projectiles;
using Content.Trauma.Common.Stack;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Knowledge.Quality;

/// <summary>
/// Handles quality interactions for construction, projectiles, etc.
/// </summary>
public sealed class QualitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    private EntityQuery<QualityComponent> _query;

    private static readonly EntProtoId CraftingKnowledge = "CraftingKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<QualityComponent>();

        // quality effects
        SubscribeLocalEvent<QualityComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<QualityComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<QualityComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<ArmorComponent, ApplyQualityEvent>(OnArmorApplyQuality);
        SubscribeLocalEvent<DestructibleComponent, ApplyQualityEvent>(OnDestructibleApplyQuality);
        SubscribeLocalEvent<DamageOtherOnHitComponent, ApplyQualityEvent>(OnSpearApplyQuality);
        SubscribeLocalEvent<GunComponent, ApplyQualityEvent>(OnGunApplyQuality);
        SubscribeLocalEvent<ProjectileComponent, ApplyQualityEvent>(OnProjectileApplyQuality);

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

    private void OnGetMeleeDamage(Entity<QualityComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= QualityModifier(ent.Comp.Quality);
    }

    private void OnGunRefreshModifiers(Entity<QualityComponent> ent, ref GunRefreshModifiersEvent args)
    {
        // 60% spread at +5, 170% at -5
        var modifier = QualityModifier(ent.Comp.Quality, 0.9f);
        args.MinAngle *= modifier;
        args.MaxAngle *= modifier;
    }

    private void OnArmorApplyQuality(Entity<ArmorComponent> ent, ref ApplyQualityEvent args)
    {
        // TODO: make this dogshit an event
        // -5 is half as good, 5 is twice as good
        var modifier = args.Modifier(0.87f);
        var coefficients = ent.Comp.Modifiers.Coefficients;
        foreach (var damageType in coefficients.Keys)
        {
            coefficients[damageType] *= modifier;
        }
        Dirty(ent);
    }

    private void OnDestructibleApplyQuality(Entity<DestructibleComponent> ent, ref ApplyQualityEvent args)
    {
        // 250% health at +5 quality
        var modifier = args.Modifier(1.2f);
        foreach (var threshold in ent.Comp.Thresholds)
        {
            if (threshold.Trigger is DamageTrigger trigger)
                trigger.Damage *= modifier;
        }
        // TODO: this cant be networked which isn't good, make a scale field?
    }

    // not specific to spears but holy class name
    private void OnSpearApplyQuality(Entity<DamageOtherOnHitComponent> ent, ref ApplyQualityEvent args)
    {
        // 180% damage at +5 quality
        ent.Comp.Damage *= args.Modifier(1.125f);
        Dirty(ent);
    }

    private void OnGunApplyQuality(Entity<GunComponent> ent, ref ApplyQualityEvent args)
    {
        _gun.RefreshModifiers(ent.AsNullable());
        // TODO: add gun jamming exploding in your face etc at low gun quality
    }

    private void OnProjectileApplyQuality(Entity<ProjectileComponent> ent, ref ApplyQualityEvent args)
    {
        ent.Comp.Damage *= args.Modifier(1.125f);
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
        Dirty(created, newComp);

        ApplyQuality((created, newComp));
    }

    /// <summary>
    /// This should only ever be run once on any entity ever.
    /// </summary>
    private void ApplyQuality(Entity<QualityComponent> ent)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);

        var ev = new ApplyQualityEvent(ent.Comp.Quality);
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

        int? lowestDelta = null;
        EntProtoId? lowestId = null;
        var knowledge = brain.Comp.KnowledgeDict;
        foreach (var (id, delta) in ent.Comp.LevelDeltas)
        {
            if (lowestDelta is not { } || (_knowledge.GetKnowledge(brain, id) is { } skill && _knowledge.GetMastery(skill.Comp) - delta < lowestDelta))
            {
                lowestDelta = delta;
                lowestId = id;
            }
        }

        int added = 0;
        if (_knowledge.GetKnowledge(brain, CraftingKnowledge) is { } crafting)
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
        ApplyQuality(ent);

        // TODO: limit skill gain based on the recipe used
        var evCrafting = new AddExperienceEvent(CraftingKnowledge, Math.Abs(ent.Comp.Quality / 2), 20);
        RaiseLocalEvent(user, ref evCrafting);

        if (lowestId is not { } actualId)
            return;

        // TODO: above
        var ev = new AddExperienceEvent(actualId, Math.Abs(ent.Comp.Quality / 2), 20);
        RaiseLocalEvent(user, ref ev);
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
public record struct ApplyQualityEvent(float Quality)
{
    public float Modifier(float power = 1.1f)
        => QualitySystem.QualityModifier(Quality, power);
}
