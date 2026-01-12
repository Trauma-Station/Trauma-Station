using Content.Goobstation.Common.Hands;
using Content.Goobstation.Common.MartialArts;
using Content.Shared._EinsteinEngines.Contests;
using Content.Shared._White.Grab;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
/// Trauma - handles interactions related to grab stages.
/// </summary>
public sealed partial class PullingSystem
{
    [Dependency] private readonly ContestsSystem _contests = default!;
    [Dependency] private readonly GrabThrownSystem _grabThrown = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private void InitializeTrauma()
    {
        SubscribeLocalEvent<PullableComponent, UpdateCanMoveEvent>(OnGrabbedMoveAttempt);
        SubscribeLocalEvent<PullableComponent, SpeakAttemptEvent>(OnGrabbedSpeakAttempt);

        SubscribeLocalEvent<PullerComponent, VirtualItemThrownEvent>(OnVirtualItemThrown);
        SubscribeLocalEvent<PullerComponent, AddCuffDoAfterEvent>(OnAddCuffDoAfterEvent);
        SubscribeLocalEvent<PullerComponent, AttackedEvent>(OnAttacked);
    }

    private void OnVirtualItemThrown(EntityUid uid, PullerComponent component, ref VirtualItemThrownEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var throwerPhysics)
            || component.Pulling == null
            || component.Pulling != args.BlockingEntity)
            return;

        if (!TryComp(args.BlockingEntity, out PullableComponent? comp))
            return;

        var stage = component.GrabStage;
        TryStopPull(args.BlockingEntity, comp, uid, true);

        if (!_combatMode.IsInCombatMode(uid)
            || HasComp<GrabThrownComponent>(args.BlockingEntity)
            || stage <= GrabStage.Soft)
            return;

        var distanceToCursor = args.Direction.Length();
        var direction = args.Direction.Normalized() * MathF.Min(distanceToCursor, component.ThrowingDistance);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 5);

        _grabThrown.Throw(args.BlockingEntity,
            uid,
            direction,
            component.GrabThrownSpeed,
            damage * component.GrabThrowDamageModifier); // Throwing the grabbed person
        _throwing.TryThrow(uid, -direction * throwerPhysics.InvMass); // Throws back the grabber
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), uid, uid);
        component.NextStageChange = _timing.CurTime.Add(TimeSpan.FromSeconds(3f)); // To avoid grab and throw spamming
    }

    private void OnAddCuffDoAfterEvent(Entity<PullerComponent> ent, ref AddCuffDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (!args.Cancelled
            && TryComp<PullableComponent>(ent.Comp.Pulling, out var comp)
            && ent.Comp.Pulling != null)
        {
            StopPulling(ent.Comp.Pulling.Value, comp);
        }
    }

    private void OnAttacked(Entity<PullerComponent> ent, ref AttackedEvent args)
    {
        if (ent.Comp.Pulling != args.User
            || ent.Comp.GrabStage < GrabStage.Soft
            || !TryComp(args.User, out PullableComponent? pullable))
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);
        if (rand.Prob(pullable.GrabEscapeChance))
            TryLowerGrabStage((args.User, pullable), (ent.Owner, ent.Comp), true);
    }

    private bool TryGrabRelease(EntityUid pullableUid, EntityUid? user, EntityUid pullerUid)
    {
        if (user == null || user.Value != pullableUid)
            return true;

        var releaseAttempt = AttemptGrabRelease(pullableUid);

        switch (releaseAttempt)
        {
            case GrabResistResult.Failed:
                _popup.PopupEntity(Loc.GetString("popup-grab-release-fail-self"),
                                pullableUid,
                                pullableUid,
                                PopupType.SmallCaution);
                return false;
            case GrabResistResult.TooSoon:
                _popup.PopupEntity(Loc.GetString("popup-grab-release-too-soon"),
                                pullableUid,
                                pullableUid,
                                PopupType.SmallCaution);
                return false;
        }

        _popup.PopupEntity(Loc.GetString("popup-grab-release-success-self"),
            pullableUid,
            pullableUid,
            PopupType.SmallCaution);

        _popup.PopupClient(
            Loc.GetString("popup-grab-release-success-puller",
                ("target", Identity.Entity(pullableUid, EntityManager))),
            pullerUid,
            pullerUid,
            PopupType.MediumCaution);

        return true;
    }
    public void StopAllPulls(EntityUid uid, bool stopPullable = true, bool stopPuller = true) // Goobstation
    {
        if (stopPullable && TryComp<PullableComponent>(uid, out var pullable) && IsPulled(uid, pullable))
            TryStopPull(uid, pullable);

        if (stopPuller && TryComp<PullerComponent>(uid, out var puller) &&
            TryComp(puller.Pulling, out PullableComponent? pullableEnt))
            TryStopPull(puller.Pulling.Value, pullableEnt);
    }

    // Goobstation - Grab Intent
    /// <summary>
    /// Trying to grab the target
    /// </summary>
    /// <param name="pullable">Target that would be grabbed</param>
    /// <param name="puller">Performer of the grab</param>
    /// <param name="ignoreCombatMode">If true, will ignore disabled combat mode</param>
    /// <param name="grabStageOverride">What stage to set the grab too from the start</param>
    /// <param name="escapeAttemptModifier">if anything what to modify the escape chance by</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    private bool TryGrab(Entity<PullableComponent?> pullable, Entity<PullerComponent?> puller, bool ignoreCombatMode = false,
        GrabStage? grabStageOverride = null, float escapeAttemptModifier = 1f)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp)
            || !Resolve(puller.Owner, ref puller.Comp)
            || HasComp<PacifiedComponent>(puller)
            || !HasComp<MobStateComponent>(pullable)
            || pullable.Comp.Puller != puller
            || puller.Comp.Pulling != pullable
            || !TryComp<MeleeWeaponComponent>(puller, out var meleeWeapon))
            return false;

        // prevent you from grabbing someone else while being grabbed
        if (TryComp<PullableComponent>(puller, out var pullerAsPullable) && pullerAsPullable.Puller != null)
            return false;

        // Don't grab without grab intent
        if (!ignoreCombatMode && !_combatMode.IsInCombatMode(puller))
            return false;

        if (_timing.CurTime < meleeWeapon.NextAttack)
            return true;

        var now = _timing.CurTime;
        var attackRateEv = new GetMeleeAttackRateEvent(puller, meleeWeapon.AttackRate, 1, puller);
        RaiseLocalEvent(puller, ref attackRateEv);
        meleeWeapon.NextAttack = now + puller.Comp.StageChangeCooldown * attackRateEv.Multipliers;
        DirtyField(puller, meleeWeapon, nameof(MeleeWeaponComponent.NextAttack));

        var beforeEvent = new BeforeHarmfulActionEvent(puller, HarmfulActionType.Grab);
        RaiseLocalEvent(pullable, beforeEvent);
        if (beforeEvent.Cancelled)
            return false;

        // It's blocking stage update, maybe better UX?
        if (puller.Comp.GrabStage == GrabStage.Suffocate)
        {
            _stamina.TakeStaminaDamage(pullable, puller.Comp.SuffocateGrabStaminaDamage);

            var comboEv = new ComboAttackPerformedEvent(puller.Owner, pullable.Owner, puller.Owner, ComboAttackType.Grab);
            RaiseLocalEvent(puller.Owner, comboEv);
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);

            return true;
        }

        // Update stage
        // TODO: Change grab stage direction
        var nextStageAddition = puller.Comp.GrabStageDirection switch
        {
            GrabStageDirection.Increase => 1,
            GrabStageDirection.Decrease => -1,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var newStage = puller.Comp.GrabStage + nextStageAddition;

        if (HasComp<MartialArtsKnowledgeComponent>(puller) // i really hate this solution holy fuck
            && TryComp<RequireProjectileTargetComponent>(pullable, out var layingDown)
            && layingDown.Active)
        {
            var ev = new CheckGrabOverridesEvent(newStage);
            RaiseLocalEvent(puller, ref ev);
            newStage = ev.Stage;
        }

        if (grabStageOverride != null)
        {
            newStage = grabStageOverride.Value;
        }

        if (!TrySetGrabStages((puller, puller.Comp), (pullable, pullable.Comp), newStage, escapeAttemptModifier))
            return false;

        var filter = Filter.Pvs(pullable, entityManager: EntityManager)
            .RemovePlayerByAttachedEntity(puller.Owner); // puller will predict it, don't flash twice
        _color.RaiseEffect(Color.Yellow, new List<EntityUid> { pullable }, filter);
        return true;
    }

    public bool TrySetGrabStages(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, GrabStage stage, float escapeAttemptModifier = 1f)
    {
        puller.Comp.GrabStage = stage;
        Dirty(puller);
        pullable.Comp.GrabStage = stage;
        Dirty(pullable);

        // harder grabs force you closer together, you can't use mind powers to choke someone 3m away
        var stageLength = stage switch
        {
            GrabStage.Hard => 0.5f,
            GrabStage.Suffocate => 0.25f,
            _ => 30f // basically use interaction range for softgrab
        };
        if (pullable.Comp.PullJointId is {} jointId &&
            TryComp<JointComponent>(pullable, out var jointComp) &&
            jointComp.GetJoints.TryGetValue(jointId, out var joint) &&
            joint is DistanceJoint distJoint &&
            distJoint.MaxLength > stageLength)
        {
            distJoint.MaxLength = stageLength;
            Dirty(pullable, jointComp);
        }

        if (!TryUpdateGrabVirtualItems(puller, pullable))
            return false;

        var filter = Filter.Pvs(puller, entityManager: EntityManager)
            .RemovePlayerByAttachedEntity(puller.Owner)
            .RemovePlayerByAttachedEntity(pullable.Owner);

        var popupType = stage switch
        {
            GrabStage.No => PopupType.Small,
            GrabStage.Soft => PopupType.Small,
            GrabStage.Hard => PopupType.MediumCaution,
            GrabStage.Suffocate => PopupType.LargeCaution,
            _ => throw new ArgumentOutOfRangeException()
        };

        var massModifier = _contests.MassContest(puller, pullable);
        pullable.Comp.GrabEscapeChance = Math.Clamp(puller.Comp.EscapeChances[stage] / massModifier * escapeAttemptModifier, 0f, 1f);

        _alertsSystem.ShowAlert(puller.Owner, puller.Comp.PullingAlert, puller.Comp.PullingAlertSeverity[stage]);
        _alertsSystem.ShowAlert(pullable.Owner, pullable.Comp.PulledAlert, pullable.Comp.PulledAlertAlertSeverity[stage]);

        _blocker.UpdateCanMove(pullable);
        _modifierSystem.RefreshMovementSpeedModifiers(puller);

        _popup.PopupEntity(Loc.GetString($"popup-grab-{puller.Comp.GrabStage.ToString().ToLower()}-target",
                ("puller", Identity.Entity(puller, EntityManager))),
            pullable,
            pullable,
            popupType);
        _popup.PopupClient(Loc.GetString($"popup-grab-{puller.Comp.GrabStage.ToString().ToLower()}-self",
                ("target", Identity.Entity(pullable, EntityManager))),
            pullable,
            puller,
            PopupType.Medium);
        _popup.PopupEntity(Loc.GetString($"popup-grab-{puller.Comp.GrabStage.ToString().ToLower()}-others",
                ("target", Identity.Entity(pullable, EntityManager)),
                ("puller", Identity.Entity(puller, EntityManager))),
            pullable,
            filter,
            true,
            popupType);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);

        var comboEv = new ComboAttackPerformedEvent(puller.Owner, pullable.Owner, puller.Owner, ComboAttackType.Grab);
        RaiseLocalEvent(puller.Owner, comboEv);
        return true;
    }

    private bool TryUpdateGrabVirtualItems(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        // Updating virtual items
        var virtualItemsCount = puller.Comp.GrabVirtualItems.Count;

        var newVirtualItemsCount = puller.Comp.NeedsHands ? 0 : 1;
        if (puller.Comp.GrabVirtualItemStageCount.TryGetValue(puller.Comp.GrabStage, out var count))
            newVirtualItemsCount += count;

        if (virtualItemsCount == newVirtualItemsCount)
            return true;

        var delta = newVirtualItemsCount - virtualItemsCount;

        // Adding new virtual items
        if (delta > 0)
        {
            for (var i = 0; i < delta; i++)
            {
                if (!_handsSystem.TryGetEmptyHand(puller.Owner, out _))
                {
                    _popup.PopupClient(Loc.GetString("popup-grab-need-hand"), puller, puller, PopupType.Medium);

                    return false;
                }

                if (!_virtual.TrySpawnVirtualItemInHand(pullable, puller.Owner, out var item, true))
                {
                    _popup.PopupClient(Loc.GetString("popup-grab-need-hand"), puller, puller, PopupType.Medium);

                    return false;
                }

                puller.Comp.GrabVirtualItems.Add(item.Value);
            }
        }

        if (delta >= 0)
        {
            Dirty(puller);
            return true;
        }

        for (var i = 0; i < Math.Abs(delta); i++)
        {
            if (i >= puller.Comp.GrabVirtualItems.Count)
                break;

            var item = puller.Comp.GrabVirtualItems[i];
            puller.Comp.GrabVirtualItems.Remove(item);
            if (TryComp<VirtualItemComponent>(item, out var virtualItem))
                _virtual.DeleteVirtualItem((item, virtualItem), puller);
        }

        return true;
    }

    /// <summary>
    /// Attempts to release entity from grab
    /// </summary>
    /// <param name="pullable">Grabbed entity</param>
    /// <returns></returns>
    private GrabResistResult AttemptGrabRelease(Entity<PullableComponent?> pullable)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp) ||
            _timing.CurTime < pullable.Comp.NextEscapeAttempt)
            return GrabResistResult.TooSoon;

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(pullable).Id);
        var rand = new System.Random(seed);
        if (rand.Prob(pullable.Comp.GrabEscapeChance))
            return GrabResistResult.Succeeded;

        pullable.Comp.NextEscapeAttempt = _timing.CurTime.Add(TimeSpan.FromSeconds(pullable.Comp.EscapeAttemptCooldown));
        Dirty(pullable.Owner, pullable.Comp);
        return GrabResistResult.Failed;
    }

    private void OnGrabbedMoveAttempt(EntityUid uid, PullableComponent component, UpdateCanMoveEvent args)
    {
        if (component.GrabStage == GrabStage.No)
            return;

        args.Cancel();
    }

    private void OnGrabbedSpeakAttempt(EntityUid uid, PullableComponent component, SpeakAttemptEvent args)
    {
        if (component.GrabStage != GrabStage.Suffocate)
            return;

        _popup.PopupEntity(Loc.GetString("popup-grabbed-cant-speak"), uid, uid, PopupType.MediumCaution); // You cant speak while someone is choking you

        args.Cancel();
    }

    /// <summary>
    /// Tries to lower grab stage for target or release it
    /// </summary>
    /// <param name="pullable">Grabbed entity</param>
    /// <param name="puller">Performer</param>
    /// <param name="ignoreCombatMode">If true, will NOT release target if combat mode is off</param>
    /// <returns></returns>
    public bool TryLowerGrabStage(Entity<PullableComponent?> pullable, Entity<PullerComponent?> puller, bool ignoreCombatMode = false)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp))
            return false;

        if (!Resolve(puller.Owner, ref puller.Comp))
            return false;

        if (pullable.Comp.Puller != puller.Owner ||
            puller.Comp.Pulling != pullable.Owner)
            return false;

        pullable.Comp.NextEscapeAttempt = _timing.CurTime.Add(TimeSpan.FromSeconds(1f));
        Dirty(pullable);
        Dirty(puller);

        if (!ignoreCombatMode && _combatMode.IsInCombatMode(puller.Owner))
        {
            TryStopPull(pullable, pullable.Comp, ignoreGrab: true);
            return true;
        }

        if (puller.Comp.GrabStage == GrabStage.No)
        {
            TryStopPull(pullable, pullable.Comp, ignoreGrab: true);
            return true;
        }

        var newStage = puller.Comp.GrabStage - 1;
        TrySetGrabStages((puller.Owner, puller.Comp), (pullable.Owner, pullable.Comp), newStage);
        return true;
    }
}
