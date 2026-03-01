// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Phones.Components;
using Content.Trauma.Shared.Phones.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Phones.Systems;

public abstract class SharedRotaryPhoneSystem : EntitySystem
{
    private static readonly ProtoId<ToolQualityPrototype> ScrewingQuality = "Screwing";
    private readonly HashSet<int> _phoneNumbers = new();
    protected const int PhoneNumberMin = 11111;
    protected const int PhoneNumberMax = 99999;
    private const int PhoneNumberPoolSize = PhoneNumberMax - PhoneNumberMin; // 88,888 possible numbers
    public const string PhoneJoint = "jointphone";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedJointSystem _joint = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneRingEvent>(OnRing);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneHungUpEvent>(OnGotHungUp);
        SubscribeLocalEvent<RotaryPhoneComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RotaryPhoneComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<RotaryPhoneComponent, EntGotRemovedFromContainerMessage>(OnPickup);
        SubscribeLocalEvent<RotaryPhoneComponent, EntGotInsertedIntoContainerMessage>(OnHangUp);
        SubscribeLocalEvent<RotaryPhoneComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<RotaryPhoneComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RotaryPhoneComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<RotaryPhoneComponent, DestructionEventArgs>(OnPhoneDestroy);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, ExaminedEvent>(OnExamineHolder);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, EntRemovedFromContainerMessage>(OnPhoneRemoveHolder);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnPhoneRemoveHolder(Entity<RotaryPhoneHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (Deleted(ent.Owner)
            || Terminating(ent.Owner)
            || Deleted(args.Entity)
            || Terminating(args.Entity))
            return;

        if (Transform(ent.Owner).MapID == MapId.Nullspace || Transform(args.Entity).MapID == MapId.Nullspace)
            return;

        if (ent.Comp.ConnectedPhone == null)
            return;

        var visuals = EnsureComp<JointVisualsComponent>(ent.Owner);
        visuals.Sprite = ent.Comp.RopeSprite;
        visuals.Target = args.Entity;
        Dirty(ent.Owner, visuals);

        var jointComp = EnsureComp<JointComponent>(ent.Owner);
        var joint = _joint.CreateDistanceJoint(ent.Owner, args.Entity, anchorA: new Vector2(0f, 0f), id: PhoneJoint);
        joint.MaxLength = 3f;
        joint.Stiffness = 0.5f;
        joint.MinLength = 0;
        Dirty(ent.Owner, jointComp);
    }

    private void OnMapInit(Entity<RotaryPhoneComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.PhoneNumber is { } existing)
        {
            _phoneNumbers.Add(existing);
            return;
        }

        if (_phoneNumbers.Count >= PhoneNumberPoolSize)
        {
            Log.Error("too many phone numbers, did you seriously put 88,888 phones on a single map?");
            return;
        }

        int numberToAdd;
        do
        {
            numberToAdd = _random.Next(PhoneNumberMin, PhoneNumberMax);
        } while (_phoneNumbers.Contains(numberToAdd));

        _phoneNumbers.Add(numberToAdd);
        ent.Comp.PhoneNumber = numberToAdd;
    }

    private void OnDestruction(Entity<RotaryPhoneHolderComponent> ent, ref DestructionEventArgs args)
    {
        if (ent.Comp.ConnectedPhone == null)
            return;

        PredictedDel(ent.Comp.ConnectedPhone);
        RemComp<JointVisualsComponent>(ent.Owner);
        RemComp<JointComponent>(ent.Owner);
        Dirty(ent);
    }

    private void OnPhoneDestroy(Entity<RotaryPhoneComponent> ent, ref DestructionEventArgs args)
    {
        DisconnectPhones(ent.Comp);
    }

    private void OnInteract(Entity<RotaryPhoneComponent> ent, ref InteractUsingEvent args)
    {
        if (_tool.HasQuality(args.Used, ScrewingQuality))
            _uiSystem.OpenUi(ent.Owner, PhoneUiKey.NameChange, args.User);
    }

    private void OnExamine(Entity<RotaryPhoneComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PhoneNumber is {} number)
            args.PushMarkup(Loc.GetString("phone-number-description", ("number", number)));
    }

    private void OnExamineHolder(Entity<RotaryPhoneHolderComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PhoneNumber is {} number)
            args.PushMarkup(Loc.GetString("phone-number-description", ("number", number)));
    }


    private void OnGetVerbs(Entity<RotaryPhoneComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanComplexInteract
            || !args.CanAccess
            || !args.CanInteract)
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("phone-speakerphone"),
            Message = Loc.GetString("phone-speakerphone-message"),
            Act = () =>
            {
                ent.Comp.SpeakerPhone = !ent.Comp.SpeakerPhone;
                Dirty(ent);

                var state = Loc.GetString(ent.Comp.SpeakerPhone ? "handheld-radio-component-on-state" : "handheld-radio-component-off-state");
                var message = Loc.GetString("phone-speakerphone-onoff", ("status", state));
                _popup.PopupPredicted(message, ent.Owner, user);
            }
        };
        args.Verbs.Add(verb);
    }

    private void OnInsertAttempt(EntityUid uid, RotaryPhoneHolderComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        if (!TryComp<RotaryPhoneComponent>(args.Item, out var phone))
            return;

        if (phone.PhoneNumber != comp.PhoneNumber)
            args.Cancelled = true;
    }


    private void OnUiClosed(Entity<RotaryPhoneComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.DialedNumber = null;
        Dirty(ent);
    }

    private void OnRing(Entity<RotaryPhoneComponent> ent, ref  PhoneRingEvent args)
    {
        ent.Comp.SoundEntity = _audio.PlayPvs(ent.Comp.RingSound, ent.Owner, AudioParams.Default.WithLoop(true))?.Entity;

        if (ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Ring);

        var name = Loc.GetString("phone-popup-ring", ("location", args.Phone.Comp.Name ?? Loc.GetString("phone-number-unknown")));

        _popup.PopupEntity(name, ent.Owner, PopupType.Medium);

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.RingPort);
        ent.Comp.ConnectedPhone = args.Phone.Owner;
    }

    private void OnPickup(Entity<RotaryPhoneComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Ear);

        ent.Comp.ConnectedPlayer = null;

        if (!TryComp<RotaryPhoneHolderComponent>(args.Container.Owner, out _))
            return;

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.PickUpPort);
        ent.Comp.Engaged = true;

        if (ent.Comp.ConnectedPhone == null || !TryComp<RotaryPhoneComponent>(ent.Comp.ConnectedPhone, out var otherPhone) )
            return;

        ConnectPhones(ent.Comp, otherPhone, ent.Owner);
    }

    private void OnHangUp(Entity<RotaryPhoneComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<ActorComponent>(args.Container.Owner, out _))
            ent.Comp.ConnectedPlayer = args.Container.Owner;

        if (!TryComp<RotaryPhoneHolderComponent>(args.Container.Owner, out var holder))
            return;

        holder.PhoneNumber = ent.Comp.PhoneNumber;
        holder.ConnectedPhone = ent.Owner;
        ent.Comp.ConnectedPhoneStand = args.Container.Owner;
        Dirty(ent.Owner, ent.Comp);

        if (ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Base);

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.HangUpPort);
        DisconnectPhones(ent.Comp);
        Dirty(ent);
    }
    private void OnGotHungUp(Entity<RotaryPhoneComponent> ent, ref PhoneHungUpEvent args)
    {
        if (!ent.Comp.Connected)
        {
            if (ent.Comp.ConnectedPhoneStand != null)
                UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Base);

            return;
        }

        var audio = _audio.PlayPvs(ent.Comp.HandUpSoundLocal, ent.Owner);
        if (audio != null)
            ent.Comp.SoundEntity = audio.Value.Entity;

        ent.Comp.ConnectedPhone = null;
        ent.Comp.Connected = false;
        Dirty(ent);
    }

    #region Helpers

    private void ConnectPhones(RotaryPhoneComponent thisPhone, RotaryPhoneComponent otherPhone, EntityUid uid)
    {
        thisPhone.Connected = true;
        otherPhone.Connected = true;
        otherPhone.ConnectedPhone = uid;

        otherPhone.SoundEntity = _audio.Stop(otherPhone.SoundEntity);
        thisPhone.SoundEntity = _audio.Stop(thisPhone.SoundEntity);
        Dirty(uid, thisPhone);
    }

    private void DisconnectPhones(RotaryPhoneComponent thisPhone)
    {
        if (thisPhone.ConnectedPhone != null)
        {
            var ev = new PhoneHungUpEvent();

            RaiseLocalEvent(thisPhone.ConnectedPhone.Value, ref ev);

            if (!thisPhone.Connected && TryComp<RotaryPhoneComponent>(thisPhone.ConnectedPhone, out var otherPhone))
            {
                if (otherPhone.SoundEntity != null)
                    otherPhone.SoundEntity = _audio.Stop(otherPhone.SoundEntity);

                otherPhone.ConnectedPhone = null;
                otherPhone.Engaged = false;
            }
        }

        if (thisPhone.SoundEntity != null)
            thisPhone.SoundEntity = _audio.Stop(thisPhone.SoundEntity);

        thisPhone.ConnectedPhone = null;
        thisPhone.Engaged = false;
        thisPhone.Connected = false;
    }

    private void UpdateAppearance(Entity<RotaryPhoneComponent?> phone, RotaryPhoneVisuals visual)
    {
        _appearance.SetData(phone, RotaryPhoneLayers.Layer, visual);
    }

    public void RaiseDeviceNetworkEvent(EntityUid? phoneStand, string portName)
    {
        if (phoneStand == null)
            return;

        _deviceLink.InvokePort(phoneStand.Value, portName);
    }

    #endregion
}
