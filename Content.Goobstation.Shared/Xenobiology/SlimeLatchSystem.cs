// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Components.Equipment;
using Content.Medical.Common.Targeting;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Trauma.Common.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Xenobiology;

// This handles any actions that slime mobs may have.
public sealed partial class SlimeLatchSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityQuery<HungerComponent> _hungerQuery = default!;
    [Dependency] private EntityQuery<SlimeComponent> _slimeQuery = default!;
    [Dependency] private EntityQuery<XenoVacuumTankComponent> _tankQuery = default!;

    private TimeSpan _updateDelay = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeLatchEvent>(OnLatchAttempt);
        SubscribeLocalEvent<SlimeComponent, SlimeLatchDoAfterEvent>(OnSlimeLatchDoAfter);

        SubscribeLocalEvent<SlimeDamageOvertimeComponent, MobStateChangedEvent>(OnMobStateChangedSOD);
        SubscribeLocalEvent<SlimeComponent, MobStateChangedEvent>(OnMobStateChangedSlime);
        SubscribeLocalEvent<SlimeComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<SlimeComponent, BeingThrownAttemptEvent>(OnBeingThrownAttempt);
        SubscribeLocalEvent<SlimeComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<SlimeComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<SlimeComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + _updateDelay;

        var query = EntityQueryEnumerator<SlimeDamageOvertimeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;

            UpdateHunger((uid, comp));
        }
    }

    private void UpdateHunger(Entity<SlimeDamageOvertimeComponent> ent)
    {
        var addedHunger = (float) ent.Comp.Damage.GetTotal();
        _damageable.ChangeDamage(ent.Owner, ent.Comp.Damage, ignoreResistances: true, targetPart: TargetBodyPart.All);

        if (ent.Comp.SourceEntityUid is { } source && _hungerQuery.TryComp(ent.Comp.SourceEntityUid, out var hunger))
        {
            _hunger.ModifyHunger(source, addedHunger, hunger);
        }
    }

    private void OnMobStateChangedSOD(Entity<SlimeDamageOvertimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || ent.Comp.SourceEntityUid is not {} source)
            return;

        if (_slimeQuery.TryComp(source, out var slime))
            Unlatch((source, slime));
    }

    private void OnMobStateChangedSlime(Entity<SlimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Unlatch(ent);
    }

    private void OnPullAttempt(Entity<SlimeComponent> ent, ref PullAttemptEvent args)
    {
        if (IsLatched(ent) && args.PullerUid != ent.Owner) // slimes can't be pulled when latched
        {
            args.Cancelled = true;
            return;
        }

        Unlatch(ent);
    }

    private void OnBeingThrownAttempt(Entity<SlimeComponent> ent, ref BeingThrownAttemptEvent args)
    {
        // can't just shove a slime off, use the doafter
        args.Cancelled |= IsLatched(ent);
    }

    private void OnUpdateCanMove(Entity<SlimeComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (IsLatched(ent))
            args.Cancel();
    }

    private void OnRemovedFromContainer(Entity<SlimeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // this check is probably useless but jic
        if (!_tankQuery.HasComp(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    private void OnInsertedIntoContainer(Entity<SlimeComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!_tankQuery.HasComp(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    private void OnLatchAttempt(SlimeLatchEvent args)
    {
        // TODO: just subscribe for SlimeComponent bruh
        var user = args.Performer;
        if (TerminatingOrDeleted(args.Target)
        || TerminatingOrDeleted(user)
        || !_slimeQuery.TryComp(user, out var slime))
            return;

        var ent = new Entity<SlimeComponent>(user, slime);

        if (IsLatched(ent))
        {
            Unlatch(ent);
            return;
        }

        if (CanLatch(ent, args.Target))
        {
            StartSlimeLatchDoAfter(ent, args.Target);
            return;
        }

        // improvement space (tm)
    }

    private bool StartSlimeLatchDoAfter(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (_mobState.IsDead(target))
        {
            var targetDeadPopup = Loc.GetString("slime-latch-fail-target-dead", ("ent", target));
            _popup.PopupClient(targetDeadPopup, ent, ent);

            return false;
        }

        if (ent.Comp.Stomach.Count >= ent.Comp.MaxContainedEntities)
        {
            var maxEntitiesPopup = Loc.GetString("slime-latch-fail-max-entities", ("ent", target));
            _popup.PopupClient(maxEntitiesPopup, ent, ent);

            return false;
        }

        var attemptPopup = Loc.GetString("slime-latch-attempt", ("slime", ent), ("ent", target));
        _popup.PopupPredicted(attemptPopup, ent, ent, PopupType.MediumCaution);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.LatchDoAfterDuration, new SlimeLatchDoAfterEvent(), ent, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        EnsureComp<BeingLatchedComponent>(target);
        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private void OnSlimeLatchDoAfter(Entity<SlimeComponent> ent, ref SlimeLatchDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (args.Handled || args.Cancelled)
        {
            RemCompDeferred<BeingLatchedComponent>(target);
            return;
        }

        Latch(ent, target);
        args.Handled = true;
    }

    #region Helpers

    public bool IsLatched(Entity<SlimeComponent> ent)
        => ent.Comp.LatchedTarget.HasValue;

    public bool IsLatched(Entity<SlimeComponent> ent, EntityUid target)
        => IsLatched(ent) && ent.Comp.LatchedTarget!.Value == target;

    public bool CanLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        return !(IsLatched(ent) // already latched
            || _mobState.IsDead(target) // target dead
            || !_actionBlocker.CanInteract(ent, target) // can't reach
            || !HasComp<MobStateComponent>(target)); // make any mob work
    }

    public bool NpcTryLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (!CanLatch(ent, target))
            return false;

        return StartSlimeLatchDoAfter(ent, target);
    }

    public void Latch(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (IsLatched(ent))
            Unlatch(ent);

        _xform.SetCoordinates(ent, Transform(target).Coordinates);
        _xform.SetParent(ent, target);

        ent.Comp.LatchedTarget = target;
        Dirty(ent);

        _actionBlocker.UpdateCanMove(ent.Owner);

        EnsureComp(target, out SlimeDamageOvertimeComponent comp);
        comp.SourceEntityUid = ent;
        Dirty(target, comp);

        _audio.PlayPredicted(ent.Comp.EatSound, ent, ent);
        _popup.PopupPredicted(Loc.GetString("slime-action-latch-success", ("slime", ent), ("target", target)), ent, ent, PopupType.SmallCaution);

        // We also need to set a new state for the slime when it's consuming,
        // this will be easy however it's important to take MobGrowthSystem into account... possibly we should use layers?
    }

    public void Unlatch(Entity<SlimeComponent> ent)
    {
        if (!IsLatched(ent))
            return;

        var target = ent.Comp.LatchedTarget!.Value;

        RemCompDeferred<BeingLatchedComponent>(target);
        RemCompDeferred<SlimeDamageOvertimeComponent>(target);

        _xform.SetParent(ent, _xform.GetParentUid(target)); // deparent it. probably.
        ent.Comp.LatchedTarget = null;
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    #endregion
}
