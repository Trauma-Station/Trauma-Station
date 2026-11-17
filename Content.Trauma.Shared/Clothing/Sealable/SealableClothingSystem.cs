// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.Cloning;
using Content.Trauma.Common.Clothing;
using Content.Trauma.Common.Inventory;
using Content.Trauma.Shared.Clothing.MultiToggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Clothing.Sealable;

/// <summary>
/// System used for sealable clothing (like modsuits)
/// </summary>
public sealed partial class SealableClothingSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ComponentTogglerSystem _toggler = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MultiToggleableClothingSystem _toggleable = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private EntityQuery<MultiToggleableClothingComponent> _toggleableQuery = default!;
    [Dependency] private EntityQuery<MultiAttachedClothingComponent> _attachedQuery = default!;
    [Dependency] private EntityQuery<SealableClothingComponent> _sealableQuery = default!;
    [Dependency] private EntityQuery<SealableClothingControlComponent> _controlQuery = default!;

    private readonly Dictionary<EntityUid, ProtoId<InventorySlotPrototype>> _toggledClothing = new();
    private readonly List<(Entity<SealableClothingControlComponent> Control, EntityUid Wearer, EntityUid User)> _queuedStartSealToggleProccess = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<SealableClothingControlComponent, GetVerbsEvent<EquipmentVerb>>(OnEquipmentVerb, held: false);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (control, wearer, user) in _queuedStartSealToggleProccess)
        {
            TryStartSealToggleProcess(control, wearer, user);
        }

        _queuedStartSealToggleProccess.Clear();
    }

    [SubscribeLocalEvent(before: new[] { typeof(MultiToggleableClothingSystem) })]
    private void OnTransfer(Entity<SealableClothingControlComponent> ent, ref InventoryItemTransferEvent args)
    {
        // If not sealed - don't care
        if (!ent.Comp.IsCurrentlySealed || !_toggleableQuery.TryComp(ent, out var toggleable))
            return;

        // Handle = true should block MultiToggleableClothingSystem from doing the transfer
        args.Handled = true;

        BreakSeal(ent, args.Source, true);

        if (!_toggleable.TransferToggleable((ent, toggleable), args.Source, args.Target, args.Slot))
            return;

        EndSealProcess(ent, args.Target, true, true);
    }

    [SubscribeLocalEvent(after: new[] { typeof(MultiToggleableClothingSystem) })]
    private void OnCopied(Entity<SealableClothingControlComponent> ent, ref EquipmentCopiedEvent args)
    {
        if (!_controlQuery.TryComp(args.OriginalItem, out var sealableComp) || !sealableComp.IsCurrentlySealed)
            return;

        EndSealProcess(ent, args.CloneEnt, true, true);
    }

    [SubscribeLocalEvent]
    private void OnPartSealingComplete(Entity<SealableClothingComponent> ent, ref ClothingPartSealCompleteEvent args)
    {
        _appearance.SetData(ent, SealableClothingVisuals.Sealed, args.IsSealed);
        _toggler.ToggleComponent(ent, args.IsSealed);
    }

    [SubscribeLocalEvent]
    private void OnControlSealingComplete(Entity<SealableClothingControlComponent> ent, ref ClothingControlSealCompleteEvent args)
    {
        _appearance.SetData(ent, SealableClothingVisuals.Sealed, args.IsSealed);
        _toggler.ToggleComponent(ent, args.IsSealed);
    }

    private void OnEquipmentVerb(Entity<SealableClothingControlComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        var user = args.User;

        if (!args.CanInteract || !args.CanInteract || args.Hands == null || !args.CanComplexInteract)
            return;

        var wearer = Transform(ent).ParentUid;

        var verbIcon = ent.Comp.IsCurrentlySealed ?
            new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/unlock.svg.192dpi.png")) :
            new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/lock.svg.192dpi.png"));

        var verb = new EquipmentVerb()
        {
            Icon = verbIcon,
            Priority = 5,
            Text = Loc.GetString(ent.Comp.VerbText),
        };

        if (args.User == wearer)
            verb.Act = () => TryStartSealToggleProcess(ent, wearer, user);
        else
            verb.Act = () => StartSealDoAfter(ent, wearer, user);

        args.Verbs.Add(verb);
    }

    private bool StartSealDoAfter(Entity<SealableClothingControlComponent> ent, EntityUid wearer, EntityUid user)
    {
        var args = new DoAfterArgs(EntityManager, user, ent.Comp.NonWearerSealingTime, new StartSealingProcessDoAfterEvent(), ent, wearer, ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 2,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        if (wearer != user)
            _popup.PopupEntity("You start the suit's sealing process", wearer, user);

        var popup = Loc.GetString("strippable-component-alert-owner-interact", ("user", Identity.Entity(user, EntityManager)), ("item", ent));
        _popup.PopupEntity(popup, wearer, wearer, PopupType.Large);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnStartSealingDoAfter(Entity<SealableClothingControlComponent> ent, ref StartSealingProcessDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } wearer)
        {
            if (!ent.Comp.IsInProcess && ent.Comp.UnequipAfterUnseal)
            {
                ent.Comp.UnequipAfterUnseal = false;
                Dirty(ent);
            }

            return;
        }

        args.Handled = true;
        // Can't start doafter inside a doafter
        _queuedStartSealToggleProccess.Add((ent, wearer, args.User));
    }

    [SubscribeLocalEvent]
    private void OnControlSealEvent(Entity<SealableClothingControlComponent> ent, ref SealClothingEvent args)
    {
        if (Transform(ent).ParentUid != args.Performer)
            return;

        args.Handled = true;

        if (ent.Comp.IsInProcess)
        {
            _popup.PopupEntity(ent.Comp.IsCurrentlySealed
                    ? Loc.GetString(ent.Comp.SealedInProcessToggleFailPopup)
                    : Loc.GetString(ent.Comp.UnsealedInProcessToggleFailPopup),
                ent,
                args.Performer);

            _audio.PlayLocal(ent.Comp.FailSound, ent, args.Performer);
            return;
        }

        TryStartSealToggleProcess(ent, args.Performer, args.Performer);
    }

    [SubscribeLocalEvent]
    private void OnSealClothingDoAfter(Entity<SealableClothingControlComponent> ent, ref SealClothingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } wearer || args.Used is not { } part)
            return;

        SealPart(part, ent, args.User, false);
        NextSealProcess(ent, wearer, args.User);
    }

    public void SealPart(Entity<SealableClothingComponent?> ent, Entity<SealableClothingControlComponent> control, EntityUid user, bool silent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.IsSealed = !control.Comp.IsCurrentlySealed;
        Dirty(ent);

        if (!silent)
        {
            if (ent.Comp.IsSealed)
                _audio.PlayPredicted(ent.Comp.SealUpSound, control, user);
            else
                _audio.PlayPredicted(ent.Comp.SealDownSound, control, user);
        }

        var ev = new ClothingPartSealCompleteEvent(ent.Comp.IsSealed);
        RaiseLocalEvent(ent, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnToggleClothingAttempt(Entity<SealableClothingControlComponent> ent, ref ToggleClothingAttemptEvent args)
    {
        // Popup if currently sealing
        if (ent.Comp.IsInProcess)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.UnsealedInProcessToggleFailPopup), ent, args.User);
            _audio.PlayLocal(ent.Comp.FailSound, ent, args.User);
            args.Cancelled = true;
            return;
        }

        var amount = GetOccuipiedSlotsAmount(ent.Owner, args.Wearer);

        if (!ent.Comp.IsCurrentlySealed)
        {
            // Seal after toggling for others
            if (amount == 0 && args.Wearer != args.User)
                StartSealDoAfter(ent, args.Wearer, args.User);
            return;
        }

        args.Cancelled = true;

        // Popup for attempting to singular unequip with full seal
        if (!args.Multiple)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.CurrentlySealedToggleFailPopup), ent, args.User);
            _audio.PlayLocal(ent.Comp.FailSound, ent, args.User);
            return;
        }

        // Otherwise its a multiple toggle so we start unseal process
        if (args.Wearer == args.User)
        {
            if (TryStartSealToggleProcess(ent, args.Wearer, args.User))
                ent.Comp.UnequipAfterUnseal = true;
            return;
        }

        if (!StartSealDoAfter(ent, args.Wearer, args.User))
            return;

        ent.Comp.UnequipAfterUnseal = true;
        Dirty(ent);
    }

    public bool TryStartSealToggleProcess(Entity<SealableClothingControlComponent, MultiToggleableClothingComponent?> ent, EntityUid wearer, EntityUid user)
    {
        if (!_toggleableQuery.Resolve(ent, ref ent.Comp2))
            return false;

        // Prevent sealing/unsealing if modsuit don't have wearer or already started process
        if (ent.Comp1.IsInProcess)
            return false;

        if (TerminatingOrDeleted(wearer))
            return false;

        var ev = new ClothingSealAttemptEvent(wearer, user);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return false;

        // All parts required to be toggled to perform sealing
        // fix; now able to unseal even if all parts not toggled
        // edge cases where a sealed part may be unequipped and you get stuck with a broke suit
        // but we still let the user manually unseal as a fallback to an impossible situation
        if (GetOccuipiedSlotsAmount((ent, ent.Comp2), wearer) != ent.Comp2.ClothingUids.Count)
        {
            if (!ent.Comp1.IsCurrentlySealed)
            {
                _popup.PopupEntity(Loc.GetString(ent.Comp1.ToggleFailedPopup), ent, user);
                _audio.PlayLocal(ent.Comp1.FailSound, ent, user);
                return false;
            }
        }

        _toggledClothing.Clear();
        _toggleable.GetToggledClothing((ent, ent.Comp2), _toggledClothing);

        foreach (var uid in _toggledClothing.Keys)
        {
            if (!_sealableQuery.HasComp(uid))
                continue;

            ent.Comp1.ProcessQueue.Enqueue(GetNetEntity(uid));
        }

        if (ent.Comp1.ProcessQueue.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp1.ToggleFailedPopup), ent, user);
            _audio.PlayLocal(ent.Comp1.FailSound, ent, user);
            return false;
        }

        Dirty(ent, ent.Comp1);
        NextSealProcess(ent, wearer, user);
        return true;
    }

    /// <summary>
    /// Iteratively seals/unseals all parts of sealable clothing
    /// </summary>
    private void NextSealProcess(Entity<SealableClothingControlComponent> ent, EntityUid wearer, EntityUid user)
    {
        // We break on fail and clear process count in EndSealProcess method
        // If nothing fails we dirty and return, skipping EndSealProcess
        // If theres nothing to dequeue we do EndSealProcess normally (assuming all parts are sealed already)
        while (ent.Comp.ProcessQueue.TryDequeue(out var netEnt))
        {
            // dont just fucking assume the fucking entity will never be null what if a dev gibbs you at 3 in the fucking morning.
            if (TerminatingOrDeleted(wearer))
                break;

            if (!TryGetEntity(netEnt, out var processingPart))
                continue;

            var part = processingPart.Value;

            if (!_sealableQuery.TryComp(part, out var sealableComp))
                continue;

            // If part is sealed when control trying to seal - it should just skip this part
            if (sealableComp.IsSealed != ent.Comp.IsCurrentlySealed)
                continue;

            var doAfterArgs = new DoAfterArgs(EntityManager, user, sealableComp.SealingTime, new SealClothingDoAfterEvent(), ent, target: wearer, used: part, showTo: wearer)
            {
                Hidden = true,
                NeedHand = false,
                RequireCanInteract = false,
                MultiplyDelay = false,
            };

            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                break;

            // This is mostly for faster seal unseal times so that the popups dont overlay on eachother
            var xform = Transform(wearer);
            var baseCoords = xform.Coordinates;
            var offsetY = 0.25f * ent.Comp.ProcessQueue.Count;
            var popupCoords = baseCoords.Offset(new Vector2(0f, offsetY));

            var popupText = Loc.GetString(
                ent.Comp.IsCurrentlySealed ? sealableComp.SealDownPopup : sealableComp.SealUpPopup,
                ("partName", Identity.Name(part, EntityManager))
            );

            var type = ent.Comp.IsCurrentlySealed ? PopupType.SmallCaution : PopupType.Small;
            _popup.PopupCoordinates(popupText, popupCoords, wearer, type);
            Dirty(ent);
            return;
        }

        EndSealProcess(ent, wearer);
    }

    /// <summary>
    /// Finishes sealing process on control
    /// If <paramref name="doSeal"/> is true, performs sealing of unsealed parts if they are toggled
    /// </summary>
    public bool EndSealProcess(Entity<SealableClothingControlComponent, MultiToggleableClothingComponent?> ent, EntityUid wearer, bool doSeal = false, bool silent = false)
    {
        ent.Comp1.ProcessQueue.Clear();
        Dirty(ent, ent.Comp1);

        if (doSeal && ent.Comp1.IsCurrentlySealed)
            return true;

        if (!_toggleableQuery.Resolve(ent, ref ent.Comp2) || ent.Comp2.ClothingUids.Count == 0)
            return false;

        var allpartsSealed = true;

        _toggledClothing.Clear();
        _toggleable.GetToggledClothing((ent, ent.Comp2), _toggledClothing);

        if (GetOccuipiedSlotsAmount((ent, ent.Comp2), wearer) < ent.Comp2.ClothingUids.Count)
            allpartsSealed = false;
        else
        {
            foreach (var part in _toggledClothing.Keys)
            {
                if (!_sealableQuery.TryComp(part, out var sealable))
                    continue;

                if (sealable.IsSealed)
                    continue;

                if (doSeal)
                {
                    SealPart((part, sealable), ent, wearer, true);
                    continue;
                }

                allpartsSealed = false;
                break;
            }
        }

        ent.Comp1.IsCurrentlySealed = allpartsSealed;
        var ev = new ClothingControlSealCompleteEvent(ent.Comp1.IsCurrentlySealed);
        RaiseLocalEvent(ent, ref ev);

        if (!silent && _net.IsServer && !TerminatingOrDeleted(wearer))
            _audio.PlayEntity(ent.Comp1.IsCurrentlySealed ? ent.Comp1.SealCompleteSound : ent.Comp1.UnsealCompleteSound, wearer, ent);

        if (ent.Comp1.UnequipAfterUnseal)
        {
            ent.Comp1.UnequipAfterUnseal = false;
            if (!allpartsSealed && _toggledClothing.Count > 0)
                _toggleable.ToggleClothing(wearer, wearer, (ent, ent.Comp2));
        }

        return allpartsSealed;
    }

    [SubscribeLocalEvent(before: new[] { typeof(MultiToggleableClothingSystem) })]
    private void OnGotUnequipped(Entity<SealableClothingControlComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        BreakSeal(ent, args.EquipTarget);
    }

    private void BreakSeal(Entity<SealableClothingControlComponent> ent, EntityUid wearer, bool silent = false)
    {
        if (!ent.Comp.IsCurrentlySealed || TerminatingOrDeleted(ent))
            return;

        ent.Comp.ProcessQueue.Clear();
        ent.Comp.IsCurrentlySealed = false;
        Dirty(ent);

        _toggledClothing.Clear();
        _toggleable.GetToggledClothing(ent.Owner, _toggledClothing);
        foreach (var part in _toggledClothing.Keys)
        {
            if (!_sealableQuery.TryComp(part, out var partSeal))
                continue;

            partSeal.IsSealed = false;
            Dirty(part, partSeal);
            var ev = new ClothingPartSealCompleteEvent(false);
            RaiseLocalEvent(part, ref ev);
        }

        if (silent)
            return;

        _popup.PopupEntity(Loc.GetString(ent.Comp.SealBrokenPopup), ent, wearer, PopupType.LargeCaution);
        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.GenericSuitWarning, ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleableUnequipAttemptSealCheck(Entity<SealableClothingControlComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        // Cancel if the control unit is being sealed.
        if (!ent.Comp.IsCurrentlySealed && !ent.Comp.IsInProcess)
            return;

        args.Reason = "sealable-clothing-sealed-toggle-fail";
        args.Cancel();
    }

    [SubscribeLocalEvent(before: new[] { typeof(MultiToggleableClothingSystem) })]
    private void OnToggleSanityChecker(Entity<SealableClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState || !ent.Comp.IsSealed)
            return;

        if (!_attachedQuery.TryComp(ent, out var attachedComp))
            return;

        if (!_controlQuery.TryComp(attachedComp.AttachedUid, out var controlComp))
            return;

        BreakSeal((attachedComp.AttachedUid, controlComp), args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SealableClothingComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.IsSealed)
            return;

        if (!_attachedQuery.TryComp(ent, out var attachedComp))
            return;

        if (TerminatingOrDeleted(attachedComp.AttachedUid) ||
            !_controlQuery.TryComp(attachedComp.AttachedUid, out var controlComp))
            return;

        BreakSeal((attachedComp.AttachedUid, controlComp), Transform(attachedComp.AttachedUid).ParentUid);
    }

    private int GetOccuipiedSlotsAmount(Entity<MultiToggleableClothingComponent?> ent, EntityUid wearer)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;

        if (ent.Comp.ClothingUids.Count == 0)
            return 0;

        var occuiped = 0;
        foreach (var (attached, slot) in ent.Comp.ClothingUids)
        {
            if (CheckSlotOccupied(wearer, attached, slot))
                occuiped++;
        }

        return occuiped;
    }

    /// <summary>
    /// Checks if an entity has a piece of clothing equipped, or if it cannot be equipped based on their body parts.
    /// </summary>
    /// <param name="equipTarget">The entity attempting to wear the clothing</param>
    /// <param name="equipment">The item to be equipped</param>
    /// <param name="slot">The slot the clothing is being equipped to</param>
    /// <returns>True if the item could not be or is equipped to this slot</returns>
    public bool CheckSlotOccupied(EntityUid equipTarget, EntityUid equipment, string slot)
    {
        // Does their species proto include the slot?
        if (!_inventory.TryGetSlotContainer(equipTarget, slot, out var slotContainer, out _))
            return true; // don't care then

        // If there's no body part associated with the slot, we cannot equip it
        if (BodyEquipmentSystem.GetInventoryPart(slot) is not { } part)
            return true;

        // If we don't have the part, we cannot equip it
        if (_part.FindBodyPart(equipTarget, part) == null)
            return true;

        // Body part is not missing, return true if item is actually equipped
        return slotContainer.ContainedEntity == equipment;
    }
}
