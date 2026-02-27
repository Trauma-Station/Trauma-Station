using System.Numerics;
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
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Server.Phones;

public sealed class RotaryPhoneSystem : EntitySystem
{

    [Dependency] private readonly SharedChatSystem _chatSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedRotaryPhoneSystem _rotaryPhoneSystem = default!;
    [Dependency] private readonly SharedJointSystem _jointSystem = default!;

    public const string PhoneJoint = "jointphone";

    public override void Initialize()
    {
        SubscribeLocalEvent<RotaryPhoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneKeypadMessage>(OnKeyPadPressed);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneKeypadClearMessage>(OnKeyPadClear);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneBookPressedMessage>(OnPhoneBookButtonPressed);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneNameChangedMessage>(OnPhoneNameChanged);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneCategoryChangedMessage>(OnPhoneCategoryChanged);
        SubscribeLocalEvent<RotaryPhoneComponent, PhoneDialedMessage>(OnDial);
        SubscribeLocalEvent<RotaryPhoneComponent, BoundUIOpenedEvent>(OnOpen);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, EntRemovedFromContainerMessage>(OnPhoneRemoveHolder);
        SubscribeLocalEvent<RotaryPhoneHolderComponent, EntInsertedIntoContainerMessage>(OnPhoneInsertHolder);
    }


    private void OnPhoneRemoveHolder(Entity<RotaryPhoneHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if(Deleted(ent.Owner) || Terminating(ent.Owner))
            return;

        var visuals = EnsureComp<JointVisualsComponent>(ent.Owner);
        visuals.Sprite = ent.Comp.RopeSprite;
        visuals.Target = args.Entity;
        Dirty(ent.Owner, visuals);

        var jointComp = EnsureComp<JointComponent>(ent.Owner);
        var joint = _jointSystem.CreateDistanceJoint(ent.Owner, args.Entity, anchorA: new Vector2(0f, 0f), id: PhoneJoint);
        joint.MaxLength = 3f;
        joint.Stiffness = 0.5f;
        joint.MinLength = 0;
        Dirty(ent.Owner, jointComp);
    }

    private void OnPhoneInsertHolder(Entity<RotaryPhoneHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if(Deleted(ent.Owner) || Terminating(ent.Owner))
            return;

        RemComp<JointVisualsComponent>(ent.Owner);
        RemComp<JointComponent>(ent.Owner);
        Dirty(ent);
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

            if (phoneComp.PhoneNumber == null || phoneComp.Category == null)
                continue;

            var phones = new PhoneData(phoneComp.Name ?? Loc.GetString("phone-number-unknown"), phoneComp.Category, phoneComp.PhoneNumber.Value);

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
        PlayPhoneSound(ent.Owner, args.Value, ent.Comp);
        ent.Comp.DialedNumber = (ent.Comp.DialedNumber ?? 0) * 10 + args.Value;
        Dirty(ent);
    }

    private void OnKeyPadClear(Entity<RotaryPhoneComponent> ent, ref PhoneKeypadClearMessage args)
    {
        ent.Comp.DialedNumber = null;
        Dirty(ent);
    }
    private void PlayPhoneSound(EntityUid uid, int number, RotaryPhoneComponent? component = null) // Stolen from nuke code
    {
        if (!Resolve(uid, ref component))
            return;

        var semitoneShift = number - 2;

        var opts = component.KeypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-7f);
        _audio.PlayPvs(component.KeypadPressSound, uid, opts);
    }

    private void OnDial(Entity<RotaryPhoneComponent> ent, ref PhoneDialedMessage args)
    {
        if (ent.Comp.ConnectedPhone == null)
        {
            var query = EntityQueryEnumerator<RotaryPhoneComponent>();
            while (query.MoveNext(out var phone, out var phoneComp))
            {
                if (ent.Comp.DialedNumber == phoneComp.PhoneNumber && phone != ent.Owner)
                {
                    DoPickupLogic(phoneComp, ent, phone);
                    break;
                }
            }
        }
        Dirty(ent);
    }

    private void OnListen(Entity<RotaryPhoneComponent> ent, ref ListenEvent args)
    {
        if(HasComp<RotaryPhoneComponent>(args.Source)
           || args.Source == ent.Owner
           || HasComp<RadioSpeakerComponent>(args.Source)
           || ent.Comp.ConnectedPhone == null
           || !ent.Comp.Connected
           || !TryComp(ent.Comp.ConnectedPhone, out RotaryPhoneComponent? otherPhoneComponent))
            return;

        var entityMeta = MetaData(args.Source);

        if (otherPhoneComponent.SpeakerPhone)
        {
            _chatSystem.TrySendInGameICMessage(ent.Comp.ConnectedPhone.Value,
                args.Message,
                InGameICChatType.Speak,
                hideChat: true,
                hideLog: true,
                checkRadioPrefix: false,
                nameOverride: entityMeta.EntityName);

            return;
        }


        if(!TryComp(otherPhoneComponent.ConnectedPlayer, out ActorComponent? actor) || otherPhoneComponent.ConnectedPlayer == null)
            return;

        var sound = _audio.ResolveSound(ent.Comp.SpeakSound);
        var soundPath = _audio.GetAudioPath(sound);

        var message = Loc.GetString("phone-speak", ("name", entityMeta.EntityName), ("message", args.Message));

        _chatManager.ChatMessageToOne(ChatChannel.Local, message, message, otherPhoneComponent.ConnectedPlayer.Value, false, actor.PlayerSession.Channel, Color.FromHex("#9956D3"), true, soundPath, -12, hidePopup: true);
    }

    #region Helpers

    private void DoPickupLogic(RotaryPhoneComponent phoneComp, Entity<RotaryPhoneComponent> ent, EntityUid phone)
    {
        if (!phoneComp.Engaged)
        {
            ent.Comp.Engaged = true;
            ent.Comp.ConnectedPhone = phone;
            phoneComp.Engaged = true;
            var audio = _audio.PlayPvs(ent.Comp.RingingSound, ent.Owner, AudioParams.Default.WithLoop(true));
            _rotaryPhoneSystem.RaiseDeviceNetworkEvent(ent.Comp.ConnectedPhoneStand, ent.Comp.OutGoingPort);
            if (audio != null)
                ent.Comp.SoundEntity = audio.Value.Entity;

            RaiseLocalEvent(phone, new PhoneRingEvent(ent.Owner, ent.Comp));
        }
        else if(ent.Comp.SoundEntity is {})
        {
            var audio = _audio.PlayPvs(ent.Comp.BusySound, ent.Owner);
            if (audio != null)
                ent.Comp.SoundEntity = audio.Value.Entity;
        }
    }

    #endregion
}
