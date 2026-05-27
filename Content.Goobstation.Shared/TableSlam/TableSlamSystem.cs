// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Actions.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Contests;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.TableSlam;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class TableSlamSystem : EntitySystem
{
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private CommonContestsSystem _contests = default!;
    [Dependency] private IRobustRandom _random = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PullerComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<PullableComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<PostTabledComponent, DisarmAttemptEvent>(OnDisarmAttemptEvent);
    }

    private void OnDisarmAttemptEvent(Entity<PostTabledComponent> ent, ref DisarmAttemptEvent args)
    {
        if(!_random.Prob(ent.Comp.ParalyzeChance))
            return;

        _stun.TryAddParalyzeDuration(ent, TimeSpan.FromSeconds(3));
        RemComp<PostTabledComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var tabledQuery = EntityQueryEnumerator<PostTabledComponent>();
        while (tabledQuery.MoveNext(out var uid, out var comp))
        {
            if (_gameTiming.CurTime >= comp.PostTabledShovableTime)
             RemComp<PostTabledComponent>(uid);
        }
    }

    private void OnMeleeHit(Entity<PullerComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.GrabStage < GrabStage.Suffocate
            || ent.Comp.Pulling == null)
            return;

        if(!TryComp<PullableComponent>(ent.Comp.Pulling, out var pullableComponent))
            return;

        if (args.Direction != null)
            return;
        if (args.HitEntities.Count is > 1 or 0)
            return;

        var target = args.HitEntities.ElementAt(0);
        if (!HasComp<BonkableComponent>(target)) // checks if its a table.
            return;

        var massContest = _contests.MassContest(ent, ent.Comp.Pulling.Value);
        var attemptChance = Math.Clamp(0.5f * massContest, 0f, 1f);
        var attemptRoundedToNearestQuarter = Math.Round(attemptChance * 4, MidpointRounding.ToEven) / 4;
        if(_random.Prob((float) attemptRoundedToNearestQuarter)) // base chance to table slam someone is 1 if your mass ratio is less than 1 then your going to have a harder time slamming somebody.
            TryTableSlam((ent.Comp.Pulling.Value, pullableComponent), ent, target);
    }

    public void TryTableSlam(Entity<PullableComponent> ent, Entity<PullerComponent> pullerEnt, EntityUid tableUid)
    {
        if(!_transform.InRange(ent.Owner.ToCoordinates(), tableUid.ToCoordinates(), 2f ))
            return;

        _standing.Down(ent);

        _pulling.TryStopPull(ent, ent.Comp, pullerEnt, ignoreGrab: true);
        _throwing.TryThrow(ent, tableUid.ToCoordinates() , ent.Comp.BasedTabledForceSpeed, animated: false, doSpin: false);
        pullerEnt.Comp.NextStageChange = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(3)); // prevent table slamming spam
        ent.Comp.BeingTabled = true;
    }

    private void OnStartCollide(Entity<PullableComponent> ent, ref StartCollideEvent args)
    {
        if(!ent.Comp.BeingTabled)
            return;

        if (!HasComp<BonkableComponent>(args.OtherEntity))
            return;

        var modifierOnGlassBreak = 1;
        if (TryComp<GlassTableComponent>(args.OtherEntity, out var glassTableComponent))
        {
            _damageable.TryChangeDamage(args.OtherEntity, glassTableComponent.TableDamage, origin: ent, targetPart: TargetBodyPart.Chest);
            _damageable.TryChangeDamage(args.OtherEntity, glassTableComponent.ClimberDamage, origin: ent);
            modifierOnGlassBreak = 2;
        }
        else
        {
            _damageable.TryChangeDamage(ent.Owner,
                new DamageSpecifier()
                {
                    DamageDict = new() { { "Blunt", ent.Comp.TabledDamage } },
                },
                targetPart: TargetBodyPart.Chest);
            _damageable.TryChangeDamage(ent.Owner,
                new DamageSpecifier()
                {
                    DamageDict = new() { { "Blunt", ent.Comp.TabledDamage } },
                });
        }

        _stamina.TakeStaminaDamage(ent, ent.Comp.TabledStaminaDamage);
        _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(3 * modifierOnGlassBreak), false);
        var postTabledComponent = EnsureComp<PostTabledComponent>(ent);
        postTabledComponent.PostTabledShovableTime = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(3));
        ent.Comp.BeingTabled = false;

        //_audio.PlayPvs("/Audio/Effects/thudswoosh.ogg", uid);
    }
}
