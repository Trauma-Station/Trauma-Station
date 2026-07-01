// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
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

namespace Content.Trauma.Shared.Strip;

public sealed partial class TraumaStrippingSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private EntityQuery<StorageComponent> _storageQuery = default!;
    [Dependency] private EntityQuery<CuffableComponent> _cuffableQuery = default!;

    private void InitializeBagAccess()
    {
        SubscribeLocalEvent<StrippingComponent, GetVerbsEvent<Verb>>(OnGetBagAccessVerbs);
        SubscribeLocalEvent<BagAccessComponent, BagAccessDoAfterEvent>(OnBagAccessDoAfter);
        SubscribeLocalEvent<BoundUIClosedEvent>(OnStorageUiClosed);
    }

    private void OnGetBagAccessVerbs(Entity<StrippingComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Target == args.User)
            return;

        // Target must have BagAccessComponent.
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
            if (!_storageQuery.HasComponent(slotEntity))
                continue;

            var capturedSlotName = slotDef.Name;
            var capturedNetEnt = GetNetEntity(slotEntity);

            var verb = new Verb
            {
                Act = () => StartBagAccess(user, target, capturedSlotName, capturedNetEnt),
                Text = Loc.GetString("trauma-bag-access-verb", ("slot", slotDef.Name)),
                Priority = -1,
            };

            args.Verbs.Add(verb);
        }
    }

    private void StartBagAccess(EntityUid user, Entity<BagAccessComponent> target, string slotName, NetEntity netBagEntity)
    {
        var delay = GetBagAccessDelay(target);
        var (_, stealth) = _strippable.GetStripTimeModifiers(user, target.Owner, null, TimeSpan.Zero);

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

        // Increment immediately; OnBagAccessDoAfter decrements on finish/cancel.
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
        EnsureComp<IgnoreUIRangeComponent>(args.User);
        _storage.OpenStorageUI(bagEntity, args.User, storage, args.Stealth);
        // Don't remove IgnoreUIRangeComponent yet, remove it when the UI closes.
        var activeComp = EnsureComp<ActiveStrippingComponent>(args.User);
        activeComp.BagAccessOpenedStorages.Add(bagEntity);
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

    private TimeSpan GetBagAccessDelay(Entity<BagAccessComponent> target)
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
