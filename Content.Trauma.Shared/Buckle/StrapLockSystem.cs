// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._DV.Carrying;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Trauma.Common.Throwing;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Buckle;

// all the loc is specific to crucifixion, so if you want to reuse this youll want to tie loc strings to the component
public sealed class StrapLockSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly NestedEffectSystem _nestedEffect = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtItem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrapLockComponent, StrapAttemptEvent>(OnStrapAttempt);
        SubscribeLocalEvent<StrapLockComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<StrapLockComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<StrapLockComponent, StrappedEvent>(OnStrapped);

        SubscribeLocalEvent<StrapLockComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<StrapLockHeldComponent, ComponentShutdown>(OnHeldShutdown);

        SubscribeLocalEvent<StrapLockHoldingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<StrapLockHoldingComponent, ComponentShutdown>(OnHoldingShutdown);

        SubscribeLocalEvent<StrapLockedComponent, InteractionAttemptEvent>(OnLockedInteractionAttempt);
        SubscribeLocalEvent<StrapLockedComponent, AttackAttemptEvent>(OnLockedAttackAttempt);
        SubscribeLocalEvent<StrapLockedComponent, ThrowAttemptEvent>(OnLockedThrowAttempt);
        SubscribeLocalEvent<StrapLockedComponent, PullAttemptEvent>(OnLockedPullAttempt);
        SubscribeLocalEvent<StrapLockedComponent, CarryAttemptEvent>(OnLockedCarryAttempt);
        SubscribeLocalEvent<StrapLockedComponent, DownAttemptEvent>(OnLockedDownAttempt);
        SubscribeLocalEvent<StrapLockedComponent, BeingThrownAttemptEvent>(OnLockedThrownAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StrapLockHoldingComponent>();
        while (query.MoveNext(out var uid, out var holding))
        {
            // person on the cross got deleted or whatever, don't care anymore
            var target = holding.Buckled;
            if (TerminatingOrDeleted(target))
            {
                RemCompDeferred(uid, holding);
                continue;
            }

            // cross got deleted or whatever while you were holding them up, person falls down
            if (TerminatingOrDeleted(holding.Strap))
            {
                StopHolding((uid, holding));
                continue;
            }

            // they got unbuckled so stop tracking it
            if (!_buckle.IsBuckled(target))
            {
                RemCompDeferred<StrapLockHeldComponent>(target);
                RemCompDeferred(uid, holding);
                continue;
            }

            // everything still exists, drop if you moved out of range
            var pos = _transform.GetMapCoordinates(uid);
            var strapPos = _transform.GetMapCoordinates(holding.Strap);
            if (!pos.InRange(strapPos, holding.Range))
                StopHolding((uid, holding));
        }
    }

    #region Event handlers

    private void OnStrapAttempt(Entity<StrapLockComponent> ent, ref StrapAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.User is not {} user)
        {
            args.Cancelled = true;
            return;
        }

        // can't raise yourself onto the cross, no troll physics
        if (user == args.Buckle.Owner)
        {
            args.Cancelled = true;
            if (args.Popup)
                _popup.PopupClient(Loc.GetString("strap-lock-self", ("strap", ent.Owner)), ent, user);
            return;
        }

        if (_hands.CountFreeHands(user) >= ent.Comp.RequiredHands)
            return;

        args.Cancelled = true;
        if (!args.Popup)
            return;

        var msg = Loc.GetString("strap-lock-need-hands", ("hands", ent.Comp.RequiredHands), ("strap", ent.Owner));
        _popup.PopupClient(msg, ent, user);
    }

    private void OnUnstrapAttempt(Entity<StrapLockComponent> ent, ref UnstrapAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.Locked)
            return;

        args.Cancelled = true;
        if (!args.Popup)
            return;

        var buckled = Identity.Entity(args.Buckle, EntityManager);
        var key = args.User == args.Buckle.Owner ? "you" : "others";
        var msg = Loc.GetString($"strap-lock-unstrap-locked-{key}", ("buckled", buckled), ("strap", ent.Owner));
        _popup.PopupClient(msg, ent, args.User);
    }

    private void OnUnstrapped(Entity<StrapLockComponent> ent, ref UnstrappedEvent args)
    {
        UnlockStrap(ent); // can't be locked if johnny is unstrapped from it

        ClearVirtualItems(ent.AsNullable());
        RemComp<StrapLockedComponent>(args.Buckle);
        if (!TryComp<StrapLockHeldComponent>(args.Buckle, out var held))
            return;

        if (args.User == held.Holder) // if you unbuckle them yourself it's safe
            held.Unsafe = false;
        RemCompDeferred(args.Buckle, held);
    }

    private void OnStrapped(Entity<StrapLockComponent> ent, ref StrappedEvent args)
    {
        // not sure how this would happen, but prevent it
        var target = args.Buckle.Owner;
        if (args.User is not {} user)
        {
            _buckle.TryUnbuckle(target, null, args.Buckle.Comp, popup: false);
            return;
        }

        ClearVirtualItems(ent.AsNullable());

        for (int i = 0; i < ent.Comp.RequiredHands; i++)
        {
            if (!_virtItem.TrySpawnVirtualItemInHand(target, user, out var virtItem))
            {
                _buckle.TryUnbuckle(target, null, args.Buckle.Comp, popup: false);
                return;
            }

            ent.Comp.VirtualItems.Add(virtItem.Value);
        }

        StartHolding(ent, user, target);
    }

    private void OnShutdown(Entity<StrapLockComponent> ent, ref ComponentShutdown args)
    {
        StopHoldingStrapped(ent);
    }

    private void OnHeldShutdown(Entity<StrapLockHeldComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer) // pvs reset shittery breaks it idc
            RemCompDeferred<StrapLockHoldingComponent>(ent.Comp.Holder);
    }

    private void OnVirtualItemDeleted(Entity<StrapLockHoldingComponent> ent, ref VirtualItemDeletedEvent args)
    {
        // pvs reset shittery breaks it idc
        if (_net.IsClient)
            return;

        if (args.BlockingEntity == ent.Comp.Buckled)
            StopHolding(ent);
    }

    private void OnHoldingShutdown(Entity<StrapLockHoldingComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer) // pvs reset shittery breaks it idc
            StopHolding(ent);
    }

    private void OnLockedInteractionAttempt(Entity<StrapLockedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLockedAttackAttempt(Entity<StrapLockedComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnLockedThrowAttempt(Entity<StrapLockedComponent> ent, ref ThrowAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnLockedPullAttempt(Entity<StrapLockedComponent> ent, ref PullAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLockedCarryAttempt(Entity<StrapLockedComponent> ent, ref CarryAttemptEvent args)
    {
        // can't carry someone nailed to a cross or being held up on it, get them down
        args.Cancelled = true;
    }

    private void OnLockedDownAttempt(Entity<StrapLockedComponent> ent, ref DownAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnLockedThrownAttempt(Entity<StrapLockedComponent> ent, ref BeingThrownAttemptEvent args)
    {
        // prevent shoving and stuff from force unbuckling
        args.Cancelled = true;
    }

    #endregion

    private void StartHolding(Entity<StrapLockComponent> ent, EntityUid user, EntityUid target)
    {
        // no holding items your hands are getting nailed
        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(target, ref ev);
        EnsureComp<StrapLockedComponent>(target); // prevent them doing anything

        var userIdent = Identity.Entity(user, EntityManager);
        var buckled = Identity.Entity(target, EntityManager);
        var you = Loc.GetString("strap-lock-raising-you", ("buckled", buckled), ("strap", ent.Owner));
        var others = Loc.GetString("strap-lock-raising-others", ("buckled", buckled), ("strap", ent.Owner), ("user", userIdent));
        _popup.PopupPredicted(you, others, target, user);

        var comp = EnsureComp<StrapLockHoldingComponent>(user);
        comp.Strap = ent;
        comp.Buckled = target;
        comp.DropEffect = ent.Comp.DropEffect;
        Dirty(user, comp);

        var held = EnsureComp<StrapLockHeldComponent>(target);
        held.Holder = user;
        Dirty(target, held);
    }

    private void StopHolding(Entity<StrapLockHoldingComponent> ent)
    {
        var target = ent.Comp.Buckled;
        if (!target.IsValid())
            return;

        ClearVirtualItems(ent.Comp.Strap);

        ent.Comp.Buckled = EntityUid.Invalid; // prevent effect/popup stacking if called multiple times
        if (CompOrNull<StrapLockComponent>(ent.Comp.Strap)?.Locked == true)
            return; // locked so don't actually do anything

        _buckle.TryUnbuckle(target, null);

        if (CompOrNull<StrapLockHeldComponent>(target)?.Unsafe != true)
            return; // no effects if it is missing the component or marked as safe

        var userIdent = Identity.Entity(ent.Owner, EntityManager);
        var buckled = Identity.Entity(target, EntityManager);
        var you = Loc.GetString("strap-lock-dropped-you", ("buckled", buckled));
        var others = Loc.GetString("strap-lock-dropped-others", ("buckled", buckled), ("user", userIdent));
        _popup.PopupPredicted(you, others, target, _player.LocalEntity); // all clients will predict it

        _nestedEffect.ApplyNestedEffect(target, ent.Comp.DropEffect);
    }

    private void StopHoldingStrapped(EntityUid uid)
    {
        if (!TryComp<StrapComponent>(uid, out var strap))
            return;

        var buckled = strap.BuckledEntities;
        foreach (var target in buckled)
        {
            RemCompDeferred<StrapLockHeldComponent>(target);
        }
    }

    private void ClearVirtualItems(Entity<StrapLockComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var item in ent.Comp.VirtualItems)
        {
            PredictedQueueDel(item);
        }

        ent.Comp.VirtualItems.Clear();
        Dirty(ent);
    }

    #region Public API

    public void UnlockStrap(Entity<StrapLockComponent> ent)
    {
        if (!ent.Comp.Locked)
            return;

        ent.Comp.Locked = false;
        Dirty(ent);

        // crucified person doesn't just fall off yet, has to be completely denailed to drop
    }

    public void LockStrap(Entity<StrapLockComponent> ent)
    {
        if (ent.Comp.Locked)
            return;

        ent.Comp.Locked = true;
        Dirty(ent);

        // the victim no longer needs to be held up
        ClearVirtualItems(ent.AsNullable());
    }

    #endregion
}
