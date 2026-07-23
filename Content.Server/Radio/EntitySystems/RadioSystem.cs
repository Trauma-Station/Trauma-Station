// <Trauma>
using Content.Goobstation.Shared.Radio;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Systems;
using Content.Shared.Chat.RadioIconsEvents;
using Content.Shared.Whitelist;
// </Trauma>
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Power.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed partial class RadioSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private RadioJobIconSystem _radioIcon = default!;
    // </Trauma>
    [Dependency] private INetManager _netMan = default!;
    [Dependency] private IReplayRecordingManager _replay = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private EntityQuery<TelecomExemptComponent> _exemptQuery = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null
            && component.Channels.Contains(args.Channel.ID)
            && _whitelist.IsWhitelistPassOrNull(args.Channel.SendWhitelist, uid)) // Goobstation - Whitelisted radio channels
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid, args.Language); // Einstein Engines - Language
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        // <Trauma> - replaced event's MsgChatMessage with the inner messages, add language obfuscation
        var msg = args.OriginalChatMsg;
        if (_ghost.CanGhostWarp(actor.PlayerSession, out _))
        {
            msg = new ChatMessage(msg)
            {
                WrappedMessage = _chatManager.PrependFollowButtonIfAppropriate(
                    msg.WrappedMessage,
                    args.MessageSource,
                    actor.PlayerSession.Channel),
            };
        }

        if (!_language.CanUnderstand(uid, args.Language.ID))
            msg = args.LanguageObfuscatedChatMsg;

        _netMan.ServerSendMessage(new MsgChatMessage { Message = msg }, actor.PlayerSession.Channel);
        // <Trauma>
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        LanguagePrototype? language = null, // Trauma
        bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, ProtoMan.Index(channel), radioSource, escapeMarkup: escapeMarkup,
            language: language); // Trauma
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        RadioChannelPrototype channel,
        EntityUid radioSource,
        LanguagePrototype? language = null, // Trauma
        bool escapeMarkup = true)
    {
        language ??= _language.GetLanguage(messageSource); // Trauma

        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        // <Goob>
        if (_radioIcon.TryGetJobIcon(messageSource, out var jobIcon, out var jobName))
        {
            var iconEvent = new TransformSpeakerJobIconEvent(messageSource, jobIcon.Value, jobName);
            RaiseLocalEvent(messageSource, ref iconEvent);

            jobIcon = iconEvent.JobIcon;
            jobName = iconEvent.JobName;
        }
        // </Goob>

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && ProtoMan.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        // var wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
        //     ("color", channel.Color),
        //     ("fontType", speech.FontId),
        //     ("fontSize", speech.FontSize),
        //     ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
        //     ("channel", $"\\[{channel.LocalizedName}\\]"),
        //     ("name", name),
        //     ("message", content));
        var wrappedMessage = _chat.WrapMessage(null, null, channel, messageSource, name, content, speech, language, null, jobIcon, jobName); // Einstein Engines - Language

        // most radios are relayed to chat, so lets parse the chat message beforehand
        // var chat = new ChatMessage(
        //     ChatChannel.Radio,
        //     message,
        //     wrappedMessage,
        //     NetEntity.Invalid,
        //     null);
        // var chatMsg = new MsgChatMessage { Message = chat };
        // var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg);
        // Goobstation - Chat Pings
        // Added GetNetEntity(messageSource), to source
        var msg = new ChatMessage(ChatChannel.Radio, content, wrappedMessage, GetNetEntity(messageSource), null);

        // Einstein Engines - Language begin
        var obfuscated = _language.ObfuscateSpeech(content, language, messageSource);
        // Goobstation - Chat Pings
        // Added GetNetEntity(messageSource), to source
        var obfuscatedWrapped = _chat.WrapMessage(null, null, channel, messageSource, name, obfuscated, speech, language, null, jobIcon, jobName);
        var notUdsMsg = new ChatMessage(ChatChannel.Radio, obfuscated, obfuscatedWrapped, GetNetEntity(messageSource), null);
        var ev = new RadioReceiveEvent(messageSource, channel, msg, notUdsMsg, language, radioSource);
        // Einstein Engines - Language end

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        RaiseLocalEvent(messageSource, ref sendAttemptEv); // Trauma
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive
                && !(HasActiveTransmitter(transform.MapID) && HasActiveTransmitter(sourceMapId))) // goob - intermap transmitters
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(msg); // Einstein Engines - Language
        _messages.Remove(message);
    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
