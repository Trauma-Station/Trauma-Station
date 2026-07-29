// <Trauma>
using Content.Goobstation.Common.Traits;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Systems;
using Content.Shared.CCVar;
using System.Globalization;
// </Trauma>
using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private void SendEntitySpeak(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language, // Trauma
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        Color? colorOverride = null // Trauma
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        // <Trauma>
        var message = FormattedMessage.RemoveMarkupOrThrow(originalMessage);  // Remove markup before transforming.
        message = FormattedMessage.EscapeText(message); // Escape after removing markup
        message = TransformSpeech(source, message, language);
        // </Trauma>
        if (message.Length == 0)
            return;

        var speech = GetSpeechVerb(source, message);

        // get the entity's apparent name (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null && ProtoMan.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);

        // Einstein Engines - Language begin
        // var wrappedMessage = Loc.GetString((speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message") + wrappedMessagePostfix, // Goob edit
        //     ("entityName", name),
        //     ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
        //     ("fontType", speech.FontId),
        //     ("fontSize", speech.FontSize),
        //     ("message", FormattedMessage.EscapeText(message)));

        var typeLOS = SpeakRespectsLOS || language.SpeechOverride.RequireLOS;
        if (!language.SpeechOverride.RequireSpeech && language.SpeechOverride.RequireLOS)
        {
            // Since this is basically an emote, make it act like an emote for identity.
            var ent = Identity.Entity(source, EntityManager);
            name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));
        }

        // The chat message wrapped in a "x says y" string.
        var wrappedMessage = WrapPublicMessage(source, name, message, speech, language: language, colorOverride);
        // The chat message obfuscated via language obfuscation.
        var obfuscated = SanitizeInGameICMessage(source, _language.ObfuscateSpeech(message, language, source), out var emoteStr, true, _configurationManager.GetCVar(CCVars.ChatPunctuation),
        (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
        || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en"));
        // The language-obfuscated message wrapped in a "x says y" string.
        var wrappedObfuscated = WrapPublicMessage(source, name, obfuscated, speech, language: language, colorOverride);
        // Einstein Engines - Language end

        SendInVoiceRange(ChatChannel.Local, name, message, wrappedMessage, obfuscated, wrappedObfuscated, source, range, // Trauma - added name and obfuscateds
            languageOverride: language, checkLOS: typeLOS, speech: speech, colorOverride: colorOverride); // Trauma

        var ev = new EntitySpokeEvent(source, message, null, false, language); // Trauma - changed second null to false, added language
        RaiseLocalEvent(source, ev, true);

        // To avoid logging any messages sent by entities that are not players, like vendors, cloning, etc.
        // Also doesn't log if hideLog is true.
        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private void SendEntityWhisper(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        LanguagePrototype language, // Trauma
        bool hideLog = false,
        bool ignoreActionBlocker = false,
        Color? colorOverride = null // Trauma
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        // <Trauma>
        var message = FormattedMessage.RemoveMarkupOrThrow(originalMessage);
        message = FormattedMessage.EscapeText(message);
        message = TransformSpeech(source, message, language);
        // </Trauma>
        if (message.Length == 0)
            return;

        // get the entity's name by visual identity (if no override provided).
        string nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));
        // get the entity's name by voice (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }
        name = FormattedMessage.EscapeText(name);

        if (!language.SpeechOverride.RequireSpeech && language.SpeechOverride.RequireLOS)
        {
            // Since this is basically an emote, make it act like an emote for identity.
            var ent = Identity.Entity(source, EntityManager);
            name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));
        }

        var languageObfuscatedMessage = SanitizeInGameICMessage(source, _language.ObfuscateSpeech(message, language, source), out var emoteStr, true, _configurationManager.GetCVar(CCVars.ChatPunctuation),
        (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
        || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en")); // Einstein Engines - Language

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue; // Won't get logged to chat, and ghosts are too far away to see the pop-up, so we just won't send it to them.

            // Goob edit start
            if (TryComp<DeafComponent>(listener, out var modifier) && language.SpeechOverride.RequireSpeech)
                continue; // blocks anyone with the deaf component from hearing.
            // Goob edit end

            // Einstein Engines - Language begin
            var canUnderstandLanguage = _language.CanUnderstand(listener, language.ID);
            // How the entity perceives the message depends on whether it can understand its language
            var perceivedMessage = canUnderstandLanguage ? message : languageObfuscatedMessage;

            // Result is the intermediate message derived from the perceived one via obfuscation
            // Wrapped message is the result wrapped in an "x says y" string
            // Floof: handle languages that require LOS
            string result, wrappedMessage;
            if (!language.SpeechOverride.RequireLOS && data.Range <= WhisperClearRange
                || _examineSystem.InRangeUnOccluded(source, listener, WhisperClearRange)
                || data.Observer)
            {
                // Scenario 1: the listener can clearly understand the message
                result = perceivedMessage;
                wrappedMessage = WrapWhisperMessage(source, "chat-manager-entity-whisper-wrap-message", name, result, language, colorOverride);
            }
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange)) // UNEDIT FROM Einstein Engines - Language // They are out of date, this has been reverted to current ChatSystem
            {
                // Scenario 2: if the listener is too far, they only hear fragments of the message
                result = ObfuscateMessageReadability(perceivedMessage);
                wrappedMessage = WrapWhisperMessage(source, "chat-manager-entity-whisper-wrap-message", nameIdentity, result, language, colorOverride);
            }
            else
            {
                if (language.SpeechOverride.RequireLOS) // Floof - If there is no LOS, listener sees nothing.
                    continue;

                // Scenario 3: If listener is too far and has no line of sight, they can't identify the whisperer's identity
                result = ObfuscateMessageReadability(perceivedMessage);
                wrappedMessage = WrapWhisperMessage(source, "chat-manager-entity-whisper-unknown-wrap-message", string.Empty, result, language, colorOverride);
            }

            _chatManager.ChatMessageToOne(ChatChannel.Whisper, result, wrappedMessage, source, false, session.Channel);
        }

        var replayWrap = WrapWhisperMessage(source, "chat-manager-entity-whisper-wrap-message", name, message, language, colorOverride);
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Whisper, message, replayWrap, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));
        // Einstein Engines - Languages end

        var ev = new EntitySpokeEvent(source, message, channel, false, language); // Trauma - changed second null to false, added language
        RaiseLocalEvent(source, ev, true);
        if (!hideLog)
            if (originalMessage == message)
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source} as {name}: {originalMessage}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source}: {originalMessage}.");
            }
            else
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source} as {name}, original: {originalMessage}, transformed: {message}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source}, original: {originalMessage}, transformed: {message}.");
            }
    }

    protected override void SendEntityEmote(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language, // Trauma
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
        )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        string name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkupOrThrow(action)));

        if (checkEmote &&
            !TryEmoteChatInput(source, action))
            return;

        // Trauma - add name, empty obfuscation strings and checkLOS
        SendInVoiceRange(ChatChannel.Emotes, name, action, wrappedMessage, "", "", source, range, author, checkLOS: EmoteRespectsLOS);
        if (!hideLog)
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source} as {name}: {action}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source}: {action}");
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled) return;
        }
        else if (!_loocEnabled) return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        // Trauma - add name, empty obf strings, language and checkLOS
        SendInVoiceRange(ChatChannel.LOOC, name, message, wrappedMessage, "", "", source, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, player.UserId, languageOverride: CommonLanguageSystem.Universal, checkLOS: LocalOOCRespectsLOS);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {source}: {message}");
    }

    private void SendDeadChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        if (!_adminManager.IsAdmin(player) && !_deadChatEnabled)
            return;

        var clients = GetDeadChatClients();
        var playerName = Name(source);
        string wrappedMessage;
        var verb = Loc.GetString(_random.Pick(GetSpeechVerb(source, message).SpeechVerbStrings)); // Trauma
        if (_adminManager.IsAdmin(player))
        {
            wrappedMessage = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("verb", verb), // Trauma
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", player.Channel.UserName),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {source}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("verb", verb), // Trauma
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", (playerName)),
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {source}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, wrappedMessage, source, hideChat, true, clients.ToList(), author: player.UserId);
    }
}
