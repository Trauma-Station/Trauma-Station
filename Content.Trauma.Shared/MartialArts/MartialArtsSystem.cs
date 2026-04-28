// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Handles most of Martial Arts Systems.
/// </summary>
public sealed partial class MartialArtsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    //[Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly EntityQuery<PhysicsComponent> _physicsQuery = default!;

    /// <summary>
    /// Minimum velocity in m/s to scale capoeira effects (like damage) with the user's level.
    /// </summary>
    public const float MinVel = 1.5f;
    public const float MinVelSquared = MinVel * MinVel;

    public override void Initialize()
    {
        base.Initialize();
        InitializeCanPerformCombo();

        SubscribeLocalEvent<GrabStagesOverrideComponent, CheckGrabOverridesEvent>(CheckGrabStageOverride);

        SubscribeLocalEvent<FastSpeedComponent, MartialArtModifyScaleEvent>(OnScaleSpeed);
        SubscribeLocalEvent<FastSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnMoveSpeed);
        SubscribeLocalEvent<SneakAttackComponent, ComboAttackPerformedEvent>(OnSneakAttackPerformed);
        SubscribeLocalEvent<SneakAttackComponent, TookDamageEvent>(OnSneakTookDamage);
        SubscribeLocalEvent<SneakAttackComponent, ComboAttemptEvent>(OnSneakComboAttempt);
        SubscribeLocalEvent<SneakAttackComponent, ComboPerformedEvent>(OnSneakComboPerformed);
        SubscribeLocalEvent<NoGunComponent, ProjectileReflectAttemptEvent>(OnProjectileHitMartialArt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var queryComboComponent = EntityQueryEnumerator<CanPerformComboComponent>();
        while (queryComboComponent.MoveNext(out var ent, out var comp))
        {
            if (comp.CurrentTarget is { } target && TerminatingOrDeleted(target))
                comp.CurrentTarget = null;

            if (_timing.CurTime < comp.ResetTime || comp.LastAttacks.Count == 0 && comp.Momentum == 0)
                continue;

            comp.LastAttacks.Clear();
            comp.Momentum = 0;
            Dirty(ent, comp);
            // TODO: get user and do _speed.RefreshMovementSpeedModifiers(user);
        }

        var kravBlockedQuery = EntityQueryEnumerator<BlockedBreathingComponent>();
        var curTime = _timing.CurTime;
        while (kravBlockedQuery.MoveNext(out var ent, out var comp))
        {
            if (curTime < comp.BlockedTime)
                continue;
            RemCompDeferred(ent, comp);
        }

        var sneakAttackQuery = EntityQueryEnumerator<SneakAttackComponent>();
        while (sneakAttackQuery.MoveNext(out var ent, out var sneakAttack))
        {
            if (sneakAttack.IsFound && _timing.CurTime >= sneakAttack.NextHidden)
            {
                sneakAttack.IsFound = false;
                Dirty(ent, sneakAttack);
            }
        }

        var comboActionsQuery = EntityQueryEnumerator<ComboActionsComponent>();
        while (comboActionsQuery.MoveNext(out var uid, out var combo))
        {
            if (combo.QueuedPrototype is not { } comboPrototype)
                continue;

            if (combo.ComboActions.TryGetValue(comboPrototype, out var actionEnt))
            {
                if (!TryComp<ActionComponent>(actionEnt, out var action) || action.Cooldown == null)
                {
                    combo.QueuedPrototype = null;
                    Dirty(uid, combo);
                }
            }
        }
    }

    private void OnSneakAttackPerformed(Entity<SneakAttackComponent> ent, ref ComboAttackPerformedEvent args)
    {
        // need to use a weapon to be found
        if (args.Weapon != args.Performer)
            SneakAttackSurprise(ent);
    }

    private void OnSneakTookDamage(Entity<SneakAttackComponent> ent, ref TookDamageEvent args)
    {
        SneakAttackSurprise(ent);
    }

    private void SneakAttackSurprise(Entity<SneakAttackComponent> ent)
    {
        ent.Comp.NextHidden = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SecondsTillHidden);
        ent.Comp.IsFound = true;
        Dirty(ent);
    }

    private void OnSneakComboAttempt(Entity<SneakAttackComponent> ent, ref ComboAttemptEvent args)
    {
        args.Cancelled |= ent.Comp.IsFound;
    }

    private void OnSneakComboPerformed(Entity<SneakAttackComponent> ent, ref ComboPerformedEvent args)
    {
        // you only get 1 combo before being revealed, make it count
        SneakAttackSurprise(ent);
    }

    private void OnMoveSpeed(Entity<FastSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);

        args.ModifySpeed(ent.Comp.MoveCurve.GetCurve(level));
        if (!_comboQuery.TryComp(ent, out var combo))
            return;
        args.ModifySpeed(1.0f + ((float) combo.Momentum) / 10.0f);
    }

    private void OnScaleSpeed(Entity<FastSpeedComponent> ent, ref MartialArtModifyScaleEvent args)
    {
        var user = args.User;
        if (!_physicsQuery.TryComp(user, out var physics) || physics.LinearVelocity.LengthSquared() < MinVelSquared)
            return;

        var level = _knowledge.GetLevel(ent.Owner);
        var modifier = ent.Comp.DamageScaleCurve.GetCurve(level);
        args.Scale *= modifier;
    }

    private void CheckGrabStageOverride(Entity<GrabStagesOverrideComponent> ent, ref CheckGrabOverridesEvent args)
    {
        if (args.Stage == GrabStage.Soft)
            args.Stage = ent.Comp.StartingStage;
    }

    private void OnProjectileHitMartialArt(Entity<NoGunComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
