// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Strip.Components;
using Content.Trauma.Shared.Strip.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Strip;

public sealed partial class TraumaStrippingSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<CuffableComponent> _cuffableQuery = default!;
    [Dependency] private EntityQuery<ItemSlotsComponent> _itemSlotsQuery = default!;
    [Dependency] private EntityQuery<QuickDrawableComponent> _quickDrawableQuery = default!;

    private readonly List<EntityUid> _bagAccessScratch = new(); // Reused buffer for UpdateBagAccess, avoid per-tick allocation

    private void InitializeStripActions()
    {
        SubscribeLocalEvent<StrippingComponent, GetVerbsEvent<Verb>>(OnGetStripActionVerbs);
        SubscribeLocalEvent<BagAccessComponent, BagAccessDoAfterEvent>(OnBagAccessDoAfter);
        SubscribeLocalEvent<BagAccessComponent, QuickDrawDoAfterEvent>(OnQuickDrawDoAfter);
        SubscribeLocalEvent<BoundUIClosedEvent>(OnStorageUiClosed);
    }

    private void UpdateBagAccess()
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveStrippingComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (active.BagAccessOpenedStorages.Count == 0)
                continue;

            if (active.NextBagAccessCheck > curTime)
                continue;

            active.NextBagAccessCheck += active.BagAccessCheckInterval;

            var userCoords = Transform(uid).Coordinates;

            // Copy since closing a bag's UI removes it from BagAccessOpenedStorages, and we can't modify the set while looping over it.
            _bagAccessScratch.Clear();
            _bagAccessScratch.AddRange(active.BagAccessOpenedStorages);

            foreach (var bagEntity in _bagAccessScratch)
            {
                if (!Exists(bagEntity))
                {
                    active.BagAccessOpenedStorages.Remove(bagEntity);
                    continue;
                }

                if (!_transform.InRange(userCoords, Transform(bagEntity).Coordinates, SharedInteractionSystem.InteractionRange))
                {
                    _ui.CloseUi(bagEntity, StorageComponent.StorageUiKey.Key, uid);
                }
            }

            if (active.BagAccessOpenedStorages.Count == 0)
                RemComp<IgnoreUIRangeComponent>(uid);
        }
    }

    private void OnGetStripActionVerbs(Entity<StrippingComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Target == args.User)
            return;

        // Target must have BagAccessComponent - used by both bag access and quickdraw verbs.
        if (!TryComp<BagAccessComponent>(args.Target, out var bagAccess))
            return;

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        var freeHands = _hands.CountFreeHands((args.User, hands));
        var active = EnsureComp<ActiveStrippingComponent>(args.User);
        if (active.ActiveCount >= freeHands)
            return;

        if (!HasComp<InventoryComponent>(args.Target))
            return;

        var user = args.User;
        var target = (args.Target, bagAccess);
        var enumerator = _inventory.GetSlotEnumerator(args.Target);
        while (enumerator.NextItem(out var slotEntity, out var slotDef))
        {
            if (_storageQuery.HasComponent(slotEntity))
            {
                var capturedSlotName = slotDef.Name;
                var capturedNetEnt = GetNetEntity(slotEntity);

                args.Verbs.Add(new Verb
                {
                    Act = () => StartBagAccess(user, target, capturedSlotName, capturedNetEnt),
                    Text = Loc.GetString("trauma-bag-access-verb", ("slot", slotDef.Name)),
                    Priority = -1,
                });
            }

            if (_quickDrawableQuery.HasComponent(slotEntity) && _itemSlotsQuery.TryComp(slotEntity, out var itemSlots))
            {
                foreach (var (slotId, slot) in itemSlots.Slots)
                {
                    if (!_itemSlots.CanEject(slotEntity, user, slot))
                        continue;

                    var capturedSlotId = slotId;
                    var capturedDrawNetEnt = GetNetEntity(slotEntity);

                    args.Verbs.Add(new Verb
                    {
                        Act = () => StartQuickDraw(user, target, capturedSlotId, capturedDrawNetEnt),
                        Text = Loc.GetString("trauma-quickdraw-verb"),
                        Priority = -1,
                    });
                }
            }
        }
    }

    private void StartBagAccess(EntityUid user, Entity<BagAccessComponent> target, string slotName, NetEntity netBagEntity)
    {
        var baseDelay = GetStripActionDelay(target);
        var (delay, stealth) = _strippable.GetStripTimeModifiers(user, target.Owner, null, baseDelay); // Use baseDelay through so thieving reduction applies

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new BagAccessDoAfterEvent(slotName, netBagEntity, stealth),
            eventTarget: target.Owner,
            target: target.Owner,
            used: null)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            DuplicateCondition = DuplicateConditions.SameTool,
            Hidden = stealth,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        // Notify alive, uncuffed targets when the doafter starts.
        if (!stealth && !_mobState.IsDead(target.Owner))
        {
            if (!TryComp<CuffableComponent>(target.Owner, out var cuffable) || cuffable.CuffedHandCount == 0)
            {
                var userName = Identity.Name(user, EntityManager);
                var friendlySlotName = Loc.GetString("trauma-bag-access-slot", ("slot", slotName));
                _popup.PopupEntity(
                    Loc.GetString("trauma-bag-access-popup", ("user", userName), ("slot", friendlySlotName)),
                    target.Owner,
                    target.Owner,
                    PopupType.LargeCaution);
            }
        }

        // Increment immediately. OnBagAccessDoAfter decrements on finish/cancel.
        var activeComp = EnsureComp<ActiveStrippingComponent>(user);
        activeComp.ActiveCount++;
        Dirty(user, activeComp);
    }

    private void OnBagAccessDoAfter(Entity<BagAccessComponent> ent, ref BagAccessDoAfterEvent args)
    {
        // Always decrement, fires on both success and cancellation.
        if (TryComp<ActiveStrippingComponent>(args.User, out var active))
            DecrementActiveCount((args.User, active));

        if (args.Cancelled || args.Handled)
            return;

        var bagEntity = GetEntity(args.BagEntity);
        if (!Exists(bagEntity))
            return;

        if (!TryComp<StorageComponent>(bagEntity, out var storage))
            return;

        // Temporarily bypass UI range checks so the user can open a bag they aren't holding.
        // UpdateBagAccess enforces our own range limit instead.
        var activeComp = EnsureComp<ActiveStrippingComponent>(args.User);
        EnsureComp<IgnoreUIRangeComponent>(args.User);
        _storage.OpenStorageUI(bagEntity, args.User, storage, args.Stealth);
        // Don't remove IgnoreUIRangeComponent yet, remove it when the UI closes.
        if (activeComp.BagAccessOpenedStorages.Count == 0)
            activeComp.NextBagAccessCheck = _timing.CurTime + activeComp.BagAccessCheckInterval;
        activeComp.BagAccessOpenedStorages.Add(bagEntity);
        args.Handled = true;
    }

    private void StartQuickDraw(EntityUid user, Entity<BagAccessComponent> target, string slotId, NetEntity netSlotEntity)
    {
        var baseDelay = GetStripActionDelay(target);
        var (delay, stealth) = _strippable.GetStripTimeModifiers(user, target.Owner, null, baseDelay); // Use baseDelay through so thieving reduction applies

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            delay,
            new QuickDrawDoAfterEvent(netSlotEntity, slotId, stealth),
            eventTarget: target.Owner,
            target: target.Owner,
            used: null)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            DuplicateCondition = DuplicateConditions.SameTool,
            Hidden = stealth,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);

        // Notify alive, uncuffed targets when the doafter starts.
        if (!stealth && !_mobState.IsDead(target.Owner))
        {
            if (!TryComp<CuffableComponent>(target.Owner, out var cuffable) || cuffable.CuffedHandCount == 0)
            {
                var userName = Identity.Name(user, EntityManager);
                _popup.PopupEntity(
                    Loc.GetString("trauma-quickdraw-popup", ("user", userName)),
                    target.Owner,
                    target.Owner,
                    PopupType.LargeCaution);
            }
        }

        // Increment immediately. OnQuickDrawDoAfter decrements on finish/cancel.
        var activeComp = EnsureComp<ActiveStrippingComponent>(user);
        activeComp.ActiveCount++;
        Dirty(user, activeComp);
    }

    private void OnQuickDrawDoAfter(Entity<BagAccessComponent> ent, ref QuickDrawDoAfterEvent args)
    {
        // Always decrement, fires on both success and cancellation.
        if (TryComp<ActiveStrippingComponent>(args.User, out var active))
            DecrementActiveCount((args.User, active));

        if (args.Cancelled || args.Handled)
            return;

        var slotEntity = GetEntity(args.SlotEntity);
        if (!Exists(slotEntity))
            return;

        if (!_itemSlots.TryGetSlot(slotEntity, args.SlotId, out var slot))
            return;

        if (!_itemSlots.CanEject(slotEntity, args.User, slot))
            return;

        // doAfter: false, otherwise ItemSlots tries to start its own doafter too and we'd get two.
        _itemSlots.TryEjectToHands(slotEntity, slot, args.User, excludeUserAudio: true, doAfter: false);
        args.Handled = true;
    }

    private void OnStorageUiClosed(BoundUIClosedEvent args)
    {
        if (args.UiKey is not StorageComponent.StorageUiKey)
            return;

        if (!TryComp<ActiveStrippingComponent>(args.Actor, out var active))
            return;

        // args.Entity is the storage entity the UI was closed on.
        if (!active.BagAccessOpenedStorages.Remove(args.Entity))
            return;

        if (active.BagAccessOpenedStorages.Count == 0)
            RemComp<IgnoreUIRangeComponent>(args.Actor);
    }

    private TimeSpan GetStripActionDelay(Entity<BagAccessComponent> target)
    {
        if (_mobState.IsDead(target.Owner))
            return target.Comp.DeadDelay;

        if (_mobState.IsCritical(target.Owner))
            return target.Comp.CuffedOrCritDelay;

        if (_cuffableQuery.TryComp(target.Owner, out var cuffable) && cuffable.CuffedHandCount > 0)
            return target.Comp.CuffedOrCritDelay;

        return target.Comp.NormalDelay;
    }
}
