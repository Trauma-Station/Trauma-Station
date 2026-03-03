// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Systems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
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

    public override void Initialize()
    {
        base.Initialize();
        InitializeCanPerformCombo();

        SubscribeLocalEvent<GrabStagesOverrideComponent, CheckGrabOverridesEvent>(CheckGrabStageOverride);

        SubscribeLocalEvent<FastSpeedComponent, MartialArtDamageModifierEvent>(OnDamageSpeed);
        SubscribeLocalEvent<FastSpeedComponent, MartialArtSpeedModifierEvent>(OnMoveSpeed);
        SubscribeLocalEvent<SneakAttackComponent, InvokeSneakAttackSurprisedEvent>(SneakAttackSurprise);
        SubscribeLocalEvent<SneakAttackComponent, CanDoSneakAttackEvent>(SneakAttackCanAttack);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var queryComboComponent = EntityQueryEnumerator<CanPerformComboComponent>();
        while (queryComboComponent.MoveNext(out var ent, out var comp))
        {
            if (comp.CurrentTarget is { } && TerminatingOrDeleted(comp.CurrentTarget.Value))
                comp.CurrentTarget = null;

            if (_timing.CurTime < comp.ResetTime || comp.LastAttacks.Count == 0 && comp.Momentum == 0)
                continue;

            comp.LastAttacks.Clear();
            comp.Momentum = 0;
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

    private void SneakAttackSurprise(Entity<SneakAttackComponent> ent, ref InvokeSneakAttackSurprisedEvent args)
    {
        ent.Comp.NextHidden = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SecondsTillHidden);
        ent.Comp.IsFound = true;
        Dirty(ent);
    }

    private void SneakAttackCanAttack(Entity<SneakAttackComponent> ent, ref CanDoSneakAttackEvent args)
    {
        args.CanSneakAttack = !ent.Comp.IsFound;
    }

    private void OnMoveSpeed(Entity<FastSpeedComponent> ent, ref MartialArtSpeedModifierEvent args)
    {
        args.Coefficient *= Math.Abs(ent.Comp.SpeedModifier);
    }

    private void OnDamageSpeed(Entity<FastSpeedComponent> ent, ref MartialArtDamageModifierEvent args)
    {
        var user = args.User;
        if (!TryComp<PhysicsComponent>(user, out var physics))
            return;

        if (ent.Comp.InvertSpeed)
            args.Coefficient *= Math.Max(10 - (physics.LinearVelocity.Length() * ent.Comp.SpeedModifier / 2), 0);
        else
            args.Coefficient *= physics.LinearVelocity.Length() * ent.Comp.SpeedModifier / 2;
    }

    private void CheckGrabStageOverride(Entity<GrabStagesOverrideComponent> ent, ref CheckGrabOverridesEvent args)
    {
        if (args.Stage == GrabStage.Soft)
            args.Stage = ent.Comp.StartingStage;
    }
}
