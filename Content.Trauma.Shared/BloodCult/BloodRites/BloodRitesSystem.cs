// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing.Systems;
using Content.Trauma.Shared.BloodCult.Spells;
using Content.Trauma.Shared.BloodCult.UI;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;

namespace Content.Trauma.Shared.BloodCult.BloodRites;

public sealed partial class BloodRitesSystem : EntitySystem
{
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private SharedComplexJointVisualsSystem _joint = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private EntityQuery<BloodstreamComponent> _bloodQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private readonly HashSet<Entity<PuddleComponent>> _puddles = new();

    private static readonly FixedPoint2 BloodDrainLimit = FixedPoint2.New(50);
    private static readonly ProtoId<ReagentPrototype>[] BloodReagents =
    [
        "Blood",
        "AmmoniaBlood",
        "InsectBlood",
        "CopperBlood",
        "ZombieBlood",
        "AlienBlood",
        "BlackBlood",
        "BloodChangeling",
    ];

    [SubscribeLocalEvent]
    private void OnPuddleDrain(Entity<PuddleComponent> ent, ref BloodRitesDrainEvent args)
    {
        if (args.Handled)
            return;

        var rites = args.Rites;
        var user = args.User;

        var amount = DrainPuddle(rites, ent);
        if (amount <= FixedPoint2.Zero)
            return;

        args.Handled = true;
        _popup.PopupEntity($"You absorbed {amount}u of blood from the puddles.", user, user);
        PlayExtractSound(rites, user);
        rites.Comp.StoredBlood += amount;
        Dirty(rites);
    }

    [SubscribeLocalEvent]
    private void OnBloodstreamDrain(Entity<BloodstreamComponent> ent, ref BloodRitesDrainEvent args)
    {
        if (args.Handled)
            return;

        var rites = args.Rites;
        var user = args.User;

        var ev = new BloodRitesExtractDoAfterEvent();
        var time = rites.Comp.BloodExtractionTime;
        var doAfterArgs = new DoAfterArgs(EntityManager, user, time, ev, eventTarget: rites, target: ent, used: rites)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        };

        rites.Comp.Extracting = _doAfter.TryStartDoAfter(doAfterArgs);
        Dirty(rites);

        if (rites.Comp.Extracting)
            BeamEffect(rites, ent, true);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnOrbDrain(Entity<BloodOrbComponent> ent, ref BloodRitesDrainEvent args)
    {
        if (args.Handled)
            return;

        var rites = args.Rites;
        var user = args.User;

        var amount = FixedPoint2.Min(ent.Comp.StoredBlood, rites.Comp.BloodExtractionAmount);
        _popup.PopupEntity($"You absorbed {amount}u of blood from the orb.", user, user);
        PlayExtractSound(rites, user);
        ent.Comp.StoredBlood -= amount;
        rites.Comp.StoredBlood += amount;
        Dirty(rites);
        Dirty(ent);
        if (ent.Comp.StoredBlood <= FixedPoint2.Zero)
            PredictedQueueDel(ent);
        else
            BeamEffect(rites, ent, true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<BloodRitesAuraComponent> rites, ref ExaminedEvent args)
    {
        args.PushMarkup($"It has [color=darkred]{rites.Comp.StoredBlood}u of blood[/color] stored.");
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<BloodRitesAuraComponent> rites, ref AfterInteractEvent args)
    {
        var user = args.User;
        if (args.Handled ||
            args.Target is not { } target ||
            target == user ||
            rites.Comp.Extracting ||
            _cult.IsCultist(target) || // no stabbing your fellow cultists
            !_delay.TryResetDelay(rites.Owner, true))
            return;

        var ev = new BloodRitesDrainEvent(rites, user);
        RaiseLocalEvent(target, ref ev);
        args.Handled = ev.Handled;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BloodRitesAuraComponent> rites, ref BloodRitesExtractDoAfterEvent args)
    {
        rites.Comp.Extracting = false;
        Dirty(rites);

        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target)
            return;

        var user = args.User;
        var name = Identity.Name(target, EntityManager);
        var blood = DrainBlood(target);
        if (blood <= FixedPoint2.Zero)
        {
            _popup.PopupEntity($"You couldn't absorb any blood from {name}!", user, user);
            return;
        }

        rites.Comp.StoredBlood += blood;
        _popup.PopupEntity($"You absorbed {blood}u of blood from {name}.", user, user);
        PlayExtractSound(rites, user);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCultistHeal(Entity<BloodRitesAuraComponent> rites, ref TouchSpellUsedEvent args)
    {
        if (rites.Comp.Extracting)
            return;

        var user = args.User;
        var target = args.Target;

        if (!CanHeal(target, user))
            return;

        if (_bloodQuery.TryComp(target, out var bloodstream))
        {
            args.Invoke |= RestoreBloodLevel(rites, user, (target, bloodstream));
        }

        if (TryComp(target, out DamageableComponent? damageable))
        {
            args.Invoke |= Heal(rites, user, (target, damageable));
        }

        if (args.Invoke && target != user)
            BeamEffect(rites, target, false);
    }

    [SubscribeLocalEvent]
    private void OnRitesMessage(Entity<BloodRitesAuraComponent> rites, ref BloodRitesMessage args)
    {
        var id = args.SelectedProto;
        if (!rites.Comp.Crafts.TryGetValue(id, out var cost) || rites.Comp.StoredBlood < cost)
            return; // malf client

        rites.Comp.StoredBlood -= cost; // incase multiple messages in the same tick ?!
        Dirty(rites);

        var remaining = rites.Comp.StoredBlood;

        PredictedDel(rites.Owner);

        var user = args.Actor;
        var item = PredictedSpawnNextToOrDrop(args.SelectedProto, user);
        _hands.TryPickup(user, item);

        var ev = new BloodRitesItemCreatedEvent(cost, remaining);
        RaiseLocalEvent(item, ref ev);
    }

    private bool Heal(Entity<BloodRitesAuraComponent> rites, EntityUid user, Entity<DamageableComponent?> target)
    {
        if (rites.Comp.StoredBlood <= FixedPoint2.Zero)
        {
            _popup.PopupEntity("Your rites are completely out of blood...", rites, user, PopupType.SmallCaution);
            return false;
        }

        var damage = _damage.GetAllDamage(target);
        var totalDamage = damage.GetTotal();
        if (totalDamage == 0)
            return false;

        var multiplier = rites.Comp.HealRatio;
        if (target.Owner == user)
            multiplier *= rites.Comp.SelfHealRatio;

        var bloodCost = totalDamage * multiplier;
        if (bloodCost > rites.Comp.StoredBlood)
            bloodCost = rites.Comp.StoredBlood;

        var healingLeft = bloodCost / multiplier;

        foreach (var (type, value) in damage.DamageDict)
        {
            if (!ProtoMan.Resolve(type, out var damageType))
                continue;

            var toHeal = value;
            if (toHeal > healingLeft)
                toHeal = healingLeft;

            _damage.ChangeDamage(target, new DamageSpecifier(damageType, -toHeal),
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAllDamaged,
                ignoreBlockers: true);

            healingLeft -= toHeal;
            if (healingLeft == 0)
                break;
        }

        rites.Comp.StoredBlood -= bloodCost;
        Dirty(rites);
        return true;
    }

    private bool CanHeal(EntityUid target, EntityUid user)
    {
        if (!_cult.IsCultist(target))
            return false;

        if (_mob.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-heal-dead"), target, user);
            return false;
        }

        return true;
    }

    private bool RestoreBloodLevel(
        Entity<BloodRitesAuraComponent> rites,
        EntityUid user,
        Entity<BloodstreamComponent> target
    )
    {
        if (target.Comp.BloodSolution is not { } sol)
            return false;

        var missingBlood = _blood.GetMissingBlood(target.AsNullable());
        if (missingBlood <= FixedPoint2.Zero)
            return false;

        if (rites.Comp.StoredBlood <= FixedPoint2.Zero)
        {
            _popup.PopupEntity("Your rites are completely out of blood...", rites, user, PopupType.SmallCaution);
            return false;
        }

        var multiplier = rites.Comp.BloodRegenerationRatio;
        if (target.Owner == user)
            multiplier *= rites.Comp.SelfHealRatio;

        var bloodCost = missingBlood * multiplier;
        if (bloodCost > rites.Comp.StoredBlood)
            bloodCost = rites.Comp.StoredBlood;

        _blood.TryModifyBleedAmount(target.AsNullable(), -3);
        _blood.TryModifyBloodLevel(target.AsNullable(), bloodCost / multiplier);

        rites.Comp.StoredBlood -= bloodCost;
        Dirty(rites);
        return true;
    }

    /// <summary>
    /// Drains 50u of blood from a mob.
    /// Returns the volume of blood removed.
    /// </summary>
    private FixedPoint2 DrainBlood(EntityUid uid)
    {
        if (!_bloodQuery.TryComp(uid, out var blood))
            return FixedPoint2.Zero;

        var ent = (uid, blood);
        var start = _blood.GetBloodstreamVolume(ent);
        _blood.TryModifyBloodLevel(ent, -BloodDrainLimit);
        var end = _blood.GetBloodstreamVolume(ent);
        return start - end;
    }

    /// <summary>
    /// Drains blood from puddle and neighboring puddles
    /// Returns the volume of blood removed
    /// </summary>
    private FixedPoint2 DrainPuddle(Entity<BloodRitesAuraComponent> rites, EntityUid uid)
    {
        var xform = Transform(uid);
        var coords = xform.Coordinates;

        if (xform.GridUid is not { } gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return FixedPoint2.Zero;

        var tile = _map.CoordinatesToTile(gridUid, grid, coords);

        var drainedBlood = FixedPoint2.Zero;

        _puddles.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, _puddles, 0.7f, flags: LookupFlags.Uncontained);
        foreach (var puddle in _puddles)
        {
            if (!_solution.TryGetDrawableSolution(puddle.Owner, out var ent, out var sol))
                continue;

            var blood = sol.SplitSolutionWithOnly(BloodDrainLimit, BloodReagents);
            if (blood.Volume > FixedPoint2.Zero)
                PredictedSpawnAtPosition(rites.Comp.PuddleDrainEffect, Transform(puddle).Coordinates);
            _solution.UpdateChemicals(ent.Value);
            drainedBlood += blood.Volume;
        }

        if (drainedBlood == FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var beamEnt = PredictedSpawnAtPosition(rites.Comp.PuddleDrainBeamEntity, coords);
        BeamEffect(rites, beamEnt, true);
        return drainedBlood;
    }

    private void BeamEffect(Entity<BloodRitesAuraComponent> ent, EntityUid target, bool reversed)
    {
        var data = new ComplexJointVisualsData("bloodrites", ent.Comp.JointSprite, ent.Comp.JointRange)
        {
            ShouldCollide = false,
            ReverseBeam = reversed,
            Duration = ent.Comp.JointDuration,
        };

        _joint.CreateJoint(target, ent, data);
    }

    private void PlayExtractSound(Entity<BloodRitesAuraComponent> rites, EntityUid user)
    {
        _audio.PlayPredicted(rites.Comp.BloodExtractSound, rites, user);
    }
}
