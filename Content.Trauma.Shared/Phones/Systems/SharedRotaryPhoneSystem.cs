using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Trauma.Shared.Phones.Components;
using Content.Trauma.Shared.Phones.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Phones.Systems;

public sealed class SharedRotaryPhoneSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> ScrewdriverTag = "Screwdriver";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLinkSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
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
        SubscribeLocalEvent<RotaryPhoneHolderComponent, DestructionEventArgs>(OnDestruction);
    }

    private void OnMapInit(Entity<RotaryPhoneComponent> ent, ref MapInitEvent args)
    {
        if(ent.Comp.PhoneNumber == null)
            ent.Comp.PhoneNumber = _random.Next(11111,99999);
    }

    private void OnDestruction(EntityUid uid, RotaryPhoneHolderComponent comp, ref DestructionEventArgs args)
    {
        QueueDel(comp.ConnectedPhone);
    }
    private void OnPhoneDestroy(Entity<RotaryPhoneComponent> ent, ref DestructionEventArgs args)
    {
        DisconnectPhones(ent.Comp);
    }

    private void OnInteract(Entity<RotaryPhoneComponent> ent, ref InteractUsingEvent args)
    {
        if (_tag.HasTag(args.Used, ScrewdriverTag))
        {
            _uiSystem.OpenUi(ent.Owner, PhoneUiKey.NameChange, args.User);
        }
    }

    private void OnExamine(Entity<RotaryPhoneComponent> ent, ref ExaminedEvent args)
    {
        if(ent.Comp.PhoneNumber != null)
            args.PushMarkup(Loc.GetString("phone-number-description", ("number", ent.Comp.PhoneNumber)));
    }

    private void OnExamineHolder(Entity<RotaryPhoneHolderComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.PhoneNumber != null)
            args.PushMarkup(Loc.GetString("phone-number-description", ("number", ent.Comp.PhoneNumber)));
    }


    private void OnGetVerbs(Entity<RotaryPhoneComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null || args.Using == null || !args.CanAccess || !args.CanInteract)
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
                _popupSystem.PopupPredicted(message, ent.Owner, user);
            }
        };
        args.Verbs.Add(verb);
    }

    private void OnInsertAttempt(EntityUid uid, RotaryPhoneHolderComponent comp, ref ItemSlotInsertAttemptEvent args)
    {
        if(!TryComp<RotaryPhoneComponent>(args.Item, out var phone))
            return;

        if(phone.PhoneNumber != comp.PhoneNumber)
            args.Cancelled = true;
    }


    private void OnUiClosed(Entity<RotaryPhoneComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.DialedNumber = null;
    }

    private void OnRing(Entity<RotaryPhoneComponent> ent, ref  PhoneRingEvent args)
    {
        var audio = _audio.PlayPvs(ent.Comp.RingSound, ent.Owner, AudioParams.Default.WithLoop(true));

        if (ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Ring);

        _popupSystem.PopupEntity(Loc.GetString("phone-popup-ring", ("location", args.otherPhoneComponent.Name ?? "Unknown")), ent.Owner, PopupType.Medium);

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.RingPort);
        ent.Comp.ConnectedPhone = args.phone;

        if(audio != null)
            ent.Comp.SoundEntity = audio.Value.Entity;
    }

    private void OnPickup(Entity<RotaryPhoneComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Ear);

        ent.Comp.ConnectedPlayer = null;

        if (!TryComp<RotaryPhoneHolderComponent>(args.Container.Owner, out var _))
            return;

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.PickUpPort);
        ent.Comp.Engaged = true;

        if(ent.Comp.ConnectedPhone == null || !TryComp<RotaryPhoneComponent>(ent.Comp.ConnectedPhone, out var otherPhone) )
            return;

        ConnectPhones(ent.Comp, otherPhone, ent.Owner);
    }

    private void OnHangUp(Entity<RotaryPhoneComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if(TryComp<ActorComponent>(args.Container.Owner, out _))
            ent.Comp.ConnectedPlayer = args.Container.Owner;

        if (!TryComp<RotaryPhoneHolderComponent>(args.Container.Owner, out var holder))
            return;

        holder.PhoneNumber = ent.Comp.PhoneNumber;
        holder.ConnectedPhone = ent.Owner;
        ent.Comp.ConnectedPhoneStand = args.Container.Owner;
        Dirty(ent.Owner, ent.Comp);

        if(ent.Comp.ConnectedPhoneStand != null)
            UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Base);

        RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.HangUpPort);
        DisconnectPhones(ent.Comp);

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
    }

    #region Helpers

    private void ConnectPhones(RotaryPhoneComponent thisPhone, RotaryPhoneComponent otherPhone, EntityUid uid)
    {
        thisPhone.Connected = true;
        otherPhone.Connected = true;
        otherPhone.ConnectedPhone = uid;

        if(otherPhone.SoundEntity != null)
            otherPhone.SoundEntity = _audio.Stop(otherPhone.SoundEntity);

        if (thisPhone.SoundEntity != null)
            thisPhone.SoundEntity = _audio.Stop(thisPhone.SoundEntity);
    }

    private void DisconnectPhones(RotaryPhoneComponent thisPhone)
    {
        if (thisPhone.ConnectedPhone != null)
        {
            RaiseLocalEvent(thisPhone.ConnectedPhone.Value, new PhoneHungUpEvent());

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
        if(phoneStand == null)
            return;

        _deviceLinkSystem.InvokePort(phoneStand.Value, portName);
    }

    #endregion
}
