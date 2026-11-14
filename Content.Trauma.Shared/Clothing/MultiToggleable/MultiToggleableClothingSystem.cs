// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Strip;
using Content.Shared.Verbs;
using Content.Trauma.Common.Cloning;
using Content.Trauma.Common.Clothing;
using Content.Trauma.Common.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Clothing.MultiToggleable;

public sealed partial class MultiToggleableClothingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedStrippableSystem _strippable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityQuery<MultiAttachedClothingComponent> _multiAttachedQuery = default!;
    [Dependency] private EntityQuery<MultiToggleableClothingComponent> _multiToggleableQuery = default!;

    // These are used to preserve items in slots that depend on toggled slots i.e. outerclothing
    private readonly Dictionary<ProtoId<InventorySlotPrototype>, EntityUid> _childSlotClothingItems = new();
    private readonly HashSet<string> _parentSlots = new();
    private readonly Dictionary<EntityUid, ProtoId<InventorySlotPrototype>> _toggledClothing = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<MultiToggleableClothingComponent, GetVerbsEvent<EquipmentVerb>>(OnGetVerbs, held: false);
    }

    [SubscribeLocalEvent]
    private void OnInserted(Entity<MultiToggleableClothingComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container == ent.Comp.Container)
            RefreshUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<MultiToggleableClothingComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == ent.Comp.Container)
            RefreshUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleableTransfer(Entity<MultiToggleableClothingComponent> ent, ref InventoryItemTransferEvent args)
    {
        if (ent.Comp.ClothingUids.Count == 0 || args.Handled)
            return;

        args.Handled = true;
        TransferToggleable(ent, args.Source, args.Target, args.Slot);
    }

    public bool TransferToggleable(Entity<MultiToggleableClothingComponent> ent, EntityUid source, EntityUid target, ProtoId<InventorySlotPrototype> slot)
    {
        // Save currenty toggled clothing
        _toggledClothing.Clear();
        GetToggledClothingAmount(ent.AsNullable(), source, _toggledClothing);

        // Transfer entities in attached clothing containers first so that they aren't left on old entity
        foreach (var (toggled, toggledSlot) in _toggledClothing)
        {
            if (!_multiAttachedQuery.TryComp(toggled, out var attached) ||
                attached.ClothingContainer.ContainedEntity is not { } overslotted)
                continue;

            _inventory.TryEquip(target, overslotted, toggledSlot, true, true);
        }

        _inventory.TryUnequip(source, slot, true, true, triggerHandContact: true);
        if (!_inventory.TryEquip(target, ent, slot, true, true, triggerHandContact: true))
            return false;

        // Toggle them after transfer
        foreach (var (toggled, toggledSlot) in _toggledClothing)
        {
            EquipClothing(target, target, ent, toggled, toggledSlot, true);
        }

        return true;
    }

    [SubscribeLocalEvent]
    private void OnAttachedTransfer(Entity<MultiAttachedClothingComponent> ent, ref InventoryItemTransferEvent args)
    {
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCopied(Entity<MultiToggleableClothingComponent> ent, ref EquipmentCopiedEvent args)
    {
        if (ent.Comp.ClothingUids.Count == 0)
            return;

        _toggledClothing.Clear();
        GetToggledClothingAmount(args.OriginalItem, args.OriginalEnt, _toggledClothing);
        foreach (var slot in _toggledClothing.Values)
        {
            var uid = ent.Comp.ClothingUids.First(x => x.Value == slot).Key;
            EquipClothing(args.CloneEnt, args.CloneEnt, ent, uid, slot, true);
        }
    }

    [SubscribeLocalEvent]
    private void OnMessage(Entity<MultiToggleableClothingComponent> ent, ref MultiToggleableClothingUiMessage args)
    {
        var wearer = Transform(ent).ParentUid;
        if (wearer != args.Actor)
            return;

        if (!TryGetEntity(args.AttachedClothingUid, out var uid) || !ent.Comp.ClothingUids.ContainsKey(uid.Value))
            return;

        ToggleClothing(wearer, wearer, ent, uid.Value);
    }

    private void OnGetVerbs(Entity<MultiToggleableClothingComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || ent.Comp.ClothingUids.Count == 0 || !args.CanComplexInteract)
            return;

        if (!_inventory.InSlotWithFlags(ent.Owner, ent.Comp.RequiredFlags))
            return;

        var wearer = Transform(ent).ParentUid;

        if (args.User != wearer && ent.Comp.StripDelay == null)
            return;

        var user = args.User;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString("toggle-clothing-verb-default"),
        };

        if (user == wearer)
            verb.Act = () => _ui.TryToggleUi(ent.Owner, MultiToggleClothingUiKey.Key, user);
        else
            verb.Act = () => StartDoAfter(ent, wearer, user);

        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnGetAttachedStripVerbs(Entity<MultiAttachedClothingComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !args.CanComplexInteract)
            return;

        if (!_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleableComp) ||
            !toggleableComp.ClothingUids.TryGetValue(ent.Owner, out var slot) || string.IsNullOrEmpty(slot))
            return;

        if (!_inventory.InSlotWithFlags(ent.Comp.AttachedUid, toggleableComp.RequiredFlags))
            return;

        var wearer = Transform(ent.Comp.AttachedUid).ParentUid;

        if (args.User != wearer && toggleableComp.StripDelay == null)
            return;

        var user = args.User;

        var verb = new EquipmentVerb()
        {
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/outfit.svg.192dpi.png")),
            Text = Loc.GetString("toggle-clothing-verb-default"),
        };

        if (user == wearer)
            verb.Act = () => ToggleClothing(wearer, user, (ent.Comp.AttachedUid, toggleableComp), ent.Owner);
        else
            verb.Act = () => StartAttachedDoAfter(ent, wearer, user);

        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnDoAfterComplete(Entity<MultiToggleableClothingComponent> ent, ref ToggleClothingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        ToggleClothing(target, args.User, ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnAttachedDoAfterComplete(Entity<MultiAttachedClothingComponent> ent, ref AttachClothingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleableComp))
            return;

        if (!toggleableComp.ClothingUids.TryGetValue(ent.Owner, out var slot) || string.IsNullOrEmpty(slot) ||
            toggleableComp.Container.Contains(ent.Owner))
            return;

        ToggleClothing(target, args.User, (ent.Comp.AttachedUid, toggleableComp), ent.Owner);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnToggleClothingAction(Entity<MultiToggleableClothingComponent> ent, ref ToggleClothingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ClothingUids.Count == 0 || args.Performer != Transform(ent).ParentUid)
            return;

        args.Handled = true;

        // If clothing have only one attached clothing (like helmets) action will just toggle it
        // If it have more attached clothings, it'll open radial menu
        if (ent.Comp.ClothingUids.Count == 1)
            ToggleClothing(args.Performer, args.Performer, ent, ent.Comp.ClothingUids.Keys.First());
        else
            _ui.TryToggleUi(ent.Owner, MultiToggleClothingUiKey.Key, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<MultiAttachedClothingComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleComp) ||
            !toggleComp.ClothingUids.TryGetValue(ent.Owner, out var slot))
            return;

        if (!_inventory.TryUnequip(Transform(ent).ParentUid, slot, force: true))
            return;

        _container.Insert(ent.Owner, toggleComp.Container, force: true);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnToggleableUnequip(Entity<MultiToggleableClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState || TerminatingOrDeleted(ent))
            return;

        _clothing.SetEquippedPrefix(ent, null);

        foreach (var (uid, slot) in ent.Comp.ClothingUids)
        {
            if (_inventory.TryGetSlotEntity(args.EquipTarget, slot, out var slotEnt) && slotEnt == uid)
                _inventory.TryUnequip(args.EquipTarget, slot, force: true, triggerHandContact: true);

            if (!ent.Comp.Container.Contains(uid))
                _container.Insert(uid, ent.Comp.Container, force: true);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoveToggleable(Entity<MultiToggleableClothingComponent> ent, ref ComponentRemove args)
    {
        if (_net.IsClient)
            return;

        foreach (var uid in ent.Comp.ClothingUids.Keys)
        {
            QueueDel(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnAttachedUnequipAttempt(Entity<MultiAttachedClothingComponent> ent, ref BeforeGettingUnequippedEvent args)
    {
        if (args.Cancelled || args.Force)
            return;

        if (!_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleable))
            return;

        ToggleClothing(args.EquipTarget, args.User, (ent.Comp.AttachedUid, toggleable), ent.Owner);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<MultiToggleableClothingComponent> ent, ref ComponentShutdown args)
    {
        foreach (var uid in ent.Comp.ClothingUids.Keys)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            QueueDel(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnAttachedShutdown(Entity<MultiAttachedClothingComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Comp.AttachedUid) ||
            !_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleComp) ||
            toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        toggleComp.ClothingUids.Remove(ent.Owner);
        Dirty(ent.Comp.AttachedUid, toggleComp);
    }

    [SubscribeLocalEvent]
    private void OnAttachedUnequip(Entity<MultiAttachedClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping && !TerminatingOrDeleted(ent.Comp.AttachedUid) &&
            _multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleComp))
            _container.Insert(ent.Owner, toggleComp.Container, force: true);

        if (ent.Comp.ClothingContainer.ContainedEntity is not { } stored || TerminatingOrDeleted(stored))
            return;

        if (!TerminatingOrDeleted(args.EquipTarget) &&
            _inventory.TryEquip(args.EquipTarget, stored, args.Slot, silent: true, triggerHandContact: true))
            return;

        _transform.AttachToGridOrMap(stored);
    }

    [SubscribeLocalEvent]
    private void OnAttachedInit(Entity<MultiAttachedClothingComponent> ent, ref ComponentInit args)
    {
        ent.Comp.ClothingContainer = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ClothingContainerId);
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<MultiToggleableClothingComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<MultiToggleableClothingComponent> ent, ref MapInitEvent args)
    {
        foreach (var uid in ent.Comp.Container.ContainedEntities)
        {
            DebugTools.Assert(ent.Comp.ClothingUids.ContainsKey(uid), "Unexpected entity present inside of a multi-toggleable clothing container.");
        }

        if (ent.Comp.ClothingUids.Count > 0)
        {
            DebugTools.AssertEqual(ent.Comp.ClothingUids.Count == ent.Comp.ClothingPrototypes.Count, "Multi-toggleable clothing is missing some expected entities.");

            foreach (var (uid, _) in ent.Comp.ClothingUids)
            {
                DebugTools.Assert(Exists(uid), "Multi-toggleable clothing is missing expected entity.");
                DebugTools.Assert(_multiAttachedQuery.TryComp(uid, out var comp), "Multi-Toggleable clothing is missing multi-attached component");
                DebugTools.Assert(comp.AttachedUid == ent.Owner, "Multi-Toggleable clothing uid mismatch");
            }

            return;
        }

        foreach (var (slot, proto) in ent.Comp.ClothingPrototypes)
        {
            if (!TrySpawnInContainer(proto, ent, ent.Comp.ContainerId, out var spawned))
                continue;

            var comp = EnsureComp<MultiAttachedClothingComponent>(spawned.Value);
            comp.AttachedUid = ent;
            Dirty(spawned.Value, comp);

            ent.Comp.ClothingUids[spawned.Value] = slot;
        }

        Dirty(ent);
    }

    private void StartDoAfter(Entity<MultiToggleableClothingComponent> ent, EntityUid wearer, EntityUid user)
    {
        if (ent.Comp.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, ent, ent.Comp.StripDelay.Value);

        var args = new DoAfterArgs(EntityManager, user, time, new ToggleClothingDoAfterEvent(), ent, wearer, ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (time > TimeSpan.Zero)
            _popup.PopupEntity("You begin toggling the suit.", wearer, user);

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", ent.Owner));
            _popup.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    private void StartAttachedDoAfter(Entity<MultiAttachedClothingComponent> ent, EntityUid wearer, EntityUid user)
    {
        if (!_multiToggleableQuery.TryComp(ent.Comp.AttachedUid, out var toggleableComp))
            return;

        if (toggleableComp.StripDelay == null)
            return;

        var (time, stealth) = _strippable.GetStripTimeModifiers(user, wearer, ent.Comp.AttachedUid, toggleableComp.StripDelay.Value * 3f / 4f); // 3/4's of toggleable

        var args = new DoAfterArgs(EntityManager, user, time, new AttachClothingDoAfterEvent(), ent, wearer, ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        if (!stealth)
        {
            var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", ent));
            _popup.PopupEntity(popup, wearer, wearer, PopupType.Large);
        }
    }

    /// <summary>
    /// Toggle function for single clothing
    /// </summary>
    public bool ToggleClothing(EntityUid wearer, EntityUid user, Entity<MultiToggleableClothingComponent> ent, EntityUid attachedUid)
    {
        if (!CanToggleClothing(ent, wearer, user, false))
            return false;

        if (!ent.Comp.ClothingUids.TryGetValue(attachedUid, out var slot) || string.IsNullOrEmpty(slot))
            return false;

        if (!ent.Comp.Container.Contains(attachedUid))
            HandleChildClothingSingleUnequip(attachedUid, wearer, user, slot);
        else
            HandleChildClothingSingleEquip(ent, attachedUid, wearer, user, slot);

        return true;
    }

    /// <summary>
    /// Toggle function for toggling multiple clothings at once
    /// </summary>
    public bool ToggleClothing(EntityUid wearer, EntityUid user, Entity<MultiToggleableClothingComponent> ent)
    {
        if (!CanToggleClothing(ent, wearer, user, true))
            return false;

        _parentSlots.Clear();
        _toggledClothing.Clear();

        GetToggledClothingAmount(ent.AsNullable(), wearer, _toggledClothing);

        if (_toggledClothing.Count == 0)
        {
            foreach (var slot in ent.Comp.ClothingUids.Values)
            {
                _parentSlots.Add(slot);
            }

            HandleChildClothing(wearer, _parentSlots, () =>
            {
                foreach (var (clothing, slot) in ent.Comp.ClothingUids)
                {
                    EquipClothing(wearer, user, ent, clothing, slot);
                }
            });
        }
        else
        {
            foreach (var slot in _toggledClothing.Values)
            {
                _parentSlots.Add(slot);
            }

            HandleChildClothing(wearer, _parentSlots, () =>
            {
                foreach (var (clothing, slot) in _toggledClothing)
                {
                    UnequipClothing(wearer, user, clothing, slot);
                }
            });
        }

        return true;
    }

    private void HandleChildClothingSingleEquip(Entity<MultiToggleableClothingComponent> ent, EntityUid clothing, EntityUid wearer, EntityUid user, ProtoId<InventorySlotPrototype> slot)
    {
        _parentSlots.Clear();
        _parentSlots.Add(slot);
        HandleChildClothing(wearer, _parentSlots, () => EquipClothing(wearer, user, ent, clothing, slot));
    }

    private void HandleChildClothingSingleUnequip(EntityUid clothing, EntityUid wearer, EntityUid user, ProtoId<InventorySlotPrototype> slot)
    {
        _parentSlots.Clear();
        _parentSlots.Add(slot);
        HandleChildClothing(wearer, _parentSlots, () => UnequipClothing(wearer, user, clothing, slot));
    }

    /// <summary>
    /// Unequips clothing in slots that depend on <paramref name="parentSlots"/>,
    /// performs <paramref name="action"/> (equip or unequip) and tries to equip child clothing again
    /// </summary>
    private void HandleChildClothing(EntityUid wearer, HashSet<string> parentSlots, Action action)
    {
        _childSlotClothingItems.Clear();
        var slots = _inventory.GetSlotEnumerator(wearer);
        while (slots.MoveNext(out _, out var slotDef))
        {
            if (slotDef.DependsOn is not { } parentSlot || !parentSlots.Contains(parentSlot))
                continue;

            if (!_inventory.TryUnequip(wearer, slotDef.Name, out var item, silent: true))
                continue;

            _childSlotClothingItems[slotDef.Name] = item.Value;
        }

        action();

        var failedAttempts = 0;
        foreach (var (slot, item) in _childSlotClothingItems)
        {
            if (!_inventory.TryEquip(wearer, item, slot, silent: true))
                failedAttempts++;
        }

        if (failedAttempts == 0)
            return;

        _popup.PopupEntity(Loc.GetString("inventory-component-dropped-from-unequip", ("items", failedAttempts)), wearer, wearer);
    }

    private bool CanToggleClothing(Entity<MultiToggleableClothingComponent> ent, EntityUid wearer, EntityUid user, bool multiple)
    {
        if (ent.Comp.ClothingUids.Count == 0)
            return false;

        var ev = new ToggleClothingAttemptEvent(wearer, user, ent, multiple);
        RaiseLocalEvent(ent, ref ev);
        return !ev.Cancelled;
    }

    private void UnequipClothing(EntityUid wearer, EntityUid user, EntityUid clothing, string slot)
    {
        if (!_inventory.TryUnequip(user, wearer, slot, force: true, triggerHandContact: true))
            return;

        // If attached have clothing in container - equip it
        if (_multiAttachedQuery.CompOrNull(clothing)?.ClothingContainer?.ContainedEntity is not { } stored)
            return;

        if (!_inventory.TryEquip(user, wearer, stored, slot, silent: true, force: true, triggerHandContact: true))
            _transform.AttachToGridOrMap(stored); // don't just keep it inside the attached piece if equipping fails
    }

    public bool EquipClothing(EntityUid wearer, EntityUid user, Entity<MultiToggleableClothingComponent> ent, EntityUid clothing, string slot, bool silent = false)
    {
        if (_inventory.TryGetSlotEntity(wearer, slot, out var currentClothing))
        {
            // Check if we need to replace current clothing
            if (!_multiAttachedQuery.TryComp(clothing, out var attachedComp) || !ent.Comp.ReplaceCurrentClothing)
            {
                if (!silent)
                    _popup.PopupEntity(Loc.GetString("toggleable-clothing-remove-first", ("entity", currentClothing)), wearer, user);
                return false;
            }

            // Check if attached clothing have container or this container not empty
            if (attachedComp.ClothingContainer.ContainedEntity != null)
                return false;

            if (_inventory.TryUnequip(user, wearer, slot, silent: true, triggerHandContact: true))
                _container.Insert(currentClothing.Value, attachedComp.ClothingContainer);
        }

        return _inventory.TryEquip(user, wearer, clothing, slot, silent, triggerHandContact: true);
    }

    /// <summary>
    /// Gets the amount of slots currently occupied by toggled clothing or that cannot equip any clothing
    /// <paramref name="parentSlots"/> (if not null) will be filled with clothing uids that are actually toggled
    /// </summary>
    public int GetToggledClothingAmount(Entity<MultiToggleableClothingComponent?> ent, EntityUid wearer,
        Dictionary<EntityUid, ProtoId<InventorySlotPrototype>>? result = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;

        // If entity don't have any attached clothings it means none toggled
        if (ent.Comp.ClothingUids.Count == 0)
            return 0;

        var toggledCount = 0;
        foreach (var (attached, slot) in ent.Comp.ClothingUids)
        {
            if (!ent.Comp.Container.Contains(attached))
                result?[attached] = slot;

            // If it is equipped or cannot be equipped, increase the amount
            if (!CheckEquipped(wearer, attached, slot))
                continue;

            toggledCount++;
        }

        return toggledCount;
    }

    /// <summary>
    /// Checks if an entity has a piece of clothing equipped, or if it cannot be equipped based on their body parts.
    /// </summary>
    /// <param name="equipment">The item to be equipped</param>
    /// <param name="equipTarget">The entity attempting to wear the clothing</param>
    /// <param name="slot">The slot the clothing is being equipped to</param>
    /// <returns>True if the item could not be or is equipped to this slot</returns>
    public bool CheckEquipped(EntityUid equipTarget, EntityUid toEquip, string slot)
    {
        // Does their species proto include the slot?
        if (!_inventory.TryGetSlotContainer(equipTarget, slot, out var slotContainer, out _))
            return true; // don't care then

        var ev = new CheckEquipmentMissingPartEvent(slot);
        RaiseLocalEvent(equipTarget, ref ev);
        if (ev.Missing)
            return true;

        // Body part is not missing, return true if item is actually equipped
        return slotContainer.ContainedEntity == toEquip;
    }

    private void RefreshUi(EntityUid toggleable)
    {
        if (!_timing.IsFirstTimePredicted || !_ui.TryGetOpenUi(toggleable, MultiToggleClothingUiKey.Key, out var bui))
            return;

        bui.Update();
    }
}
