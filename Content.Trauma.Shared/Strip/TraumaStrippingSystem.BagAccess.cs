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
using Content.Trauma.Common.Storage;
using Content.Trauma.Shared.Strip.Components;
using Content.Trauma.Shared.Strip.Events;

namespace Content.Trauma.Shared.Strip;

public sealed partial class TraumaStrippingSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<CuffableComponent> _cuffableQuery = default!;
    [Dependency] private EntityQuery<ItemSlotsComponent> _slotsQuery = default!;
    [Dependency] private EntityQuery<QuickDrawableComponent> _quickDrawableQuery = default!;

    [SubscribeLocalEvent]
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
            if (_storageQuery.HasComp(slotEntity))
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

            if (_quickDrawableQuery.HasComp(slotEntity) && _slotsQuery.TryComp(slotEntity, out var itemSlots))
            {
                foreach (var (slotId, slot) in itemSlots.Slots)
                {
                    if (!_slots.CanEject(slotEntity, slot, user))
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

    [SubscribeLocalEvent]
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

        AddAccessOverride(args.User, bagEntity);
        _storage.OpenStorageUI(bagEntity, args.User, storage, args.Stealth);
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

    [SubscribeLocalEvent]
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

        if (!_slots.TryGetSlot(slotEntity, args.SlotId, out var slot))
            return;

        if (!_slots.CanEject(slotEntity, slot, args.User))
            return;

        // doAfter: false, otherwise ItemSlots tries to start its own doafter too and we'd get two.
        _slots.TryEjectToHands(slotEntity, slot, args.User, excludeUserAudio: true, doAfter: false);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAccessibleOverride(Entity<AccessibleOverrideComponent> ent, ref AccessibleOverrideEvent args)
    {
        if (args.User != ent.Owner || args.Handled || !ent.Comp.Allowed.Contains(args.Target))
            return;

        args.Handled = true;
        args.Accessible = true;
    }

    [SubscribeLocalEvent]
    private void OnStorageClosed(Entity<AccessibleOverrideComponent> ent, ref StorageClosedEvent args)
    {
        // have to do the doafter again if you close it or leave range
        RemoveAccessOverride(ent, args.Target);
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

    public void AddAccessOverride(EntityUid user, EntityUid target)
    {
        var comp = EnsureComp<AccessibleOverrideComponent>(user);
        if (comp.Allowed.Add(target))
            Dirty(user, comp);
    }

    public void RemoveAccessOverride(Entity<AccessibleOverrideComponent> ent, EntityUid target)
    {
        if (!ent.Comp.Allowed.Remove(target))
            return;

        if (ent.Comp.Allowed.Count > 0)
            Dirty(ent);
        else
            RemComp(ent, ent.Comp);
    }
}
