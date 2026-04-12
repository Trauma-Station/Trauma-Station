// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Physics;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Content.Trauma.Shared.Phones.Components;
using Content.Trauma.Shared.Phones.Events;
using Content.Trauma.Shared.Phones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Phones;

public sealed class RotaryPhoneSystem : SharedRotaryPhoneSystem
{
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RotaryPhoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneKeypadMessage>(OnKeyPadPressed);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneKeypadClearMessage>(OnKeyPadClear);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneBookPressedMessage>(OnPhoneBookButtonPressed);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneNameChangedMessage>(OnPhoneNameChanged);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneCategoryChangedMessage>(OnPhoneCategoryChanged);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneDialedMessage>(OnDial);
        SubscribeLocalEvent<RotaryPhoneComponent, BoundUIOpenedEvent>(OnOpen);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneHungUpEvent>(OnGotHungUp);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, EntInsertedIntoContainerMessage>(OnPhoneInsertHolder);
    }

    private void OnGotHungUp(Entity<RotaryPhoneComponent> ent, ref PhoneHungUpEvent args)
    {
        if (!ent.Comp.Connected)
        {
            if (ent.Comp.ConnectedPhoneStand != null)
                UpdateAppearance(ent.Comp.ConnectedPhoneStand.Value, RotaryPhoneVisuals.Base);
            return;
        }

        ent.Comp.SoundEntity = _audio.PlayPvs(ent.Comp.HandUpSoundLocal, ent.Owner, AudioParams.Default.WithMaxDistance(2.5f))?.Entity;

        ent.Comp.ConnectedPhone = null;
        ent.Comp.Connected = false;
        Dirty(ent);
    }

    private void OnPhoneInsertHolder(Entity<RotaryPhoneHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (Deleted(ent.Owner) || Terminating(ent.Owner))
            return;

        RemComp<JointVisualsComponent>(ent.Owner);
        RemComp<JointComponent>(ent.Owner);
        Dirty(ent);

        // Basically on map init set the phones name to whatever the holders name is so it can be changed in mapping
        if (!TryComp<RotaryPhoneComponent>(args.Entity, out var phone) || phone.Name != null)
            return;

        phone.Name = ent.Comp.Name;
    }

    private void OnPhoneCategoryChanged(Entity<RotaryPhoneComponent> ent, ref PhoneCategoryChangedMessage args)
    {
        ent.Comp.Category = args.Value;
    }

    private void OnPhoneNameChanged(Entity<RotaryPhoneComponent> ent, ref PhoneNameChangedMessage args)
    {
        ent.Comp.Name = args.Value;
    }

    private void OnOpen(Entity<RotaryPhoneComponent> ent, ref BoundUIOpenedEvent args)
    {
        var state = new GoobPhoneBuiState(GetAllPhoneData());
        _ui.SetUiState(ent.Owner, PhoneUiKey.Key, state);
    }

    private List<PhoneData> GetAllPhoneData()
    {
        var data = new List<PhoneData>();
        var query = EntityQueryEnumerator<RotaryPhoneComponent, TransformComponent>();

        while (query.MoveNext(out _, out var phoneComp, out var xform))
        {
            if (xform.MapID == MapId.Nullspace)
                continue;

            if (phoneComp.PhoneNumber is not {} number|| phoneComp.Category is not {} category)
                continue;

            var phones = new PhoneData(phoneComp.Name ?? Loc.GetString("phone-number-unknown"), category, number);

            data.Add(phones);
        }

        return data;
    }

    private void OnPhoneBookButtonPressed(Entity<RotaryPhoneComponent> ent, ref PhoneBookPressedMessage args)
    {
        ent.Comp.DialedNumber = args.Value;
        Dirty(ent);
    }

    private void OnKeyPadPressed(Entity<RotaryPhoneComponent> ent, ref PhoneKeypadMessage args)
    {
        if (ent.Comp.DialedNumber > PhoneNumberMax)
            return;

        PlayPhoneSound(ent.AsNullable(), args.Value);
        ent.Comp.DialedNumber = (ent.Comp.DialedNumber ?? 0) * 10 + args.Value;
        Dirty(ent);
    }

    private void OnKeyPadClear(Entity<RotaryPhoneComponent> ent, ref PhoneKeypadClearMessage args)
    {
        ent.Comp.DialedNumber = null;
        Dirty(ent);
    }
    private void PlayPhoneSound(Entity<RotaryPhoneComponent?> ent, int number) // Stolen from nuke code
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var semitoneShift = number - 2;

        var opts = ent.Comp.KeypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-7f);
        _audio.PlayPvs(ent.Comp.KeypadPressSound, ent.Owner, opts.WithMaxDistance(1f));
    }

    private void OnDial(Entity<RotaryPhoneComponent> ent, ref PhoneDialedMessage args)
    {
        if (ent.Comp.ConnectedPhone != null)
            return;

        var query = EntityQueryEnumerator<RotaryPhoneComponent>();
        while (query.MoveNext(out var phone, out var phoneComp))
        {
            if (ent.Comp.DialedNumber == phoneComp.PhoneNumber && phone != ent.Owner)
            {
                DoCallLogic(phoneComp, ent, phone);
                break;
            }
        }
    }

    private void OnListen(Entity<RotaryPhoneComponent> ent, ref ListenEvent args)
    {
        if (HasComp<RotaryPhoneComponent>(args.Source)
           || args.Source == ent.Owner
           || HasComp<RadioSpeakerComponent>(args.Source)
           || ent.Comp.ConnectedPhone is not {} connected
           || !TryComp(connected, out RotaryPhoneComponent? otherPhoneComponent))
            return;

        var entityMeta = MetaData(args.Source);

        if (otherPhoneComponent.SpeakerPhone)
        {
            _chat.TrySendInGameICMessage(connected,
                args.Message,
                InGameICChatType.Speak,
                hideChat: true,
                hideLog: true,
                checkRadioPrefix: false,
                nameOverride: entityMeta.EntityName);

            return;
        }


        if (!TryComp(otherPhoneComponent.ConnectedPlayer, out ActorComponent? actor) || otherPhoneComponent.ConnectedPlayer == null)
            return;

        var sound = _audio.ResolveSound(ent.Comp.SpeakSound);
        var soundPath = _audio.GetAudioPath(sound);

        var message = Loc.GetString("phone-speak", ("name", entityMeta.EntityName), ("message", args.Message));

        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, otherPhoneComponent.ConnectedPlayer.Value, false, actor.PlayerSession.Channel, Color.FromHex("#9956D3"), true, soundPath, -12, hidePopup: true);
    }

    #region Helpers

    private void DoCallLogic(RotaryPhoneComponent phoneComp, Entity<RotaryPhoneComponent> ent, EntityUid phone)
    {
        if (!phoneComp.Engaged && phoneComp.ConnectedPhone is null)
        {
            ent.Comp.ConnectedPhone = phone;
            ent.Comp.SoundEntity = _audio.PlayPredicted(ent.Comp.RingingSound, ent.Owner, ent.Owner, AudioParams.Default.WithLoop(true).WithMaxDistance(2.5f))?.Entity;
            RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.OutGoingPort);

            var ev = new PhoneRingEvent(ent);

            RaiseLocalEvent(phone, ref ev);
        }
        else if (ent.Comp.SoundEntity is null)
        {
            ent.Comp.SoundEntity = _audio.PlayPvs(ent.Comp.BusySound, ent.Owner, AudioParams.Default.WithMaxDistance(2.5f))?.Entity;
        }
    }

    #endregion
}
