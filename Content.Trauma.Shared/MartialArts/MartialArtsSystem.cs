// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
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
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        UpdateModifiers();

        var queryComboComponent = EntityQueryEnumerator<CanPerformComboComponent>();
        while (queryComboComponent.MoveNext(out var ent, out var comp))
        {
            if (comp.CurrentTarget is { } target && TerminatingOrDeleted(target))
                comp.CurrentTarget = null;

            if (_timing.CurTime < comp.ResetTime || comp.LastAttacks.Count == 0)
                continue;

            comp.LastAttacks.Clear();
            Dirty(ent, comp);
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

    [SubscribeLocalEvent]
    private void OnSneakAttackPerformed(Entity<SneakAttackComponent> ent, ref ComboAttackPerformedEvent args)
    {
        // need to use a weapon to be found
        if (args.Weapon != args.Performer)
            SneakAttackSurprise(ent);
    }

    [SubscribeLocalEvent]
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

    [SubscribeLocalEvent]
    private void OnSneakComboAttempt(Entity<SneakAttackComponent> ent, ref ComboAttemptEvent args)
    {
        args.Cancelled |= ent.Comp.IsFound;
    }

    [SubscribeLocalEvent]
    private void OnSneakComboPerformed(Entity<SneakAttackComponent> ent, ref ComboPerformedEvent args)
    {
        // you only get 1 combo before being revealed, make it count
        SneakAttackSurprise(ent);
    }

    [SubscribeLocalEvent]
    private void OnScaleSpeed(Entity<FastSpeedComponent> ent, ref MartialArtModifyScaleEvent args)
    {
        var velocity = _physicsQuery.TryComp(args.User, out var physics)
            ? physics.LinearVelocity.Length()
            : 0f;

        var comp = ent.Comp;
        args.Scale *= Math.Clamp(velocity * comp.VelocityPowerMultiplier, comp.MinPower, comp.MaxPower);
    }

    [SubscribeLocalEvent]
    private void OnTryForceStand(Entity<ForceStandStaminaComponent> ent, ref TryForceStandEvent args)
    {
        args.Stamina *= ent.Comp.Multiplier;
    }

    [SubscribeLocalEvent]
    private void CheckGrabStageOverride(Entity<GrabStagesOverrideComponent> ent, ref CheckGrabOverridesEvent args)
    {
        if (args.Stage == GrabStage.Soft)
            args.Stage = ent.Comp.StartingStage;
    }

    [SubscribeLocalEvent]
    private void OnProjectileHitMartialArt(Entity<NoGunComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
