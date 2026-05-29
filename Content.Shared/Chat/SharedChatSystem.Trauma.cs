using System.Text;
using Content.Goobstation.Common.Loudspeaker;
using Content.Goobstation.Shared.Loudspeaker.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Speech;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Chat;

public abstract partial class SharedChatSystem
{
    [Dependency] private IGameTiming _timing = default!;

    public readonly Color DefaultSpeakColor = Color.White;

    /// <summary>
    ///     Wraps a message sent by the specified entity into an "x says y" string.
    /// </summary>
    public string WrapPublicMessage(EntityUid source, string name, string message, SpeechVerbPrototype speech, LanguagePrototype? language = null, Color? colorOverride = null)
    {
        var wrapId = speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message";
        return WrapMessage(wrapId, InGameICChatType.Speak, source, name, message, speech, language, colorOverride);
    }

    /// <summary>
    ///     Wraps a message whispered by the specified entity into an "x whispers y" string.
    /// </summary>
    public string WrapWhisperMessage(EntityUid source, LocId defaultWrap, string entityName, string message, LanguagePrototype? language = null, Color? colorOverride = null)
    {
        return WrapMessage(defaultWrap, InGameICChatType.Whisper, source, entityName, message, null, language, colorOverride);
    }

    /// <summary>
    ///     Wraps a message sent by the specified entity into the specified wrap string.
    /// </summary>
    public string WrapMessage(LocId wrapId, InGameICChatType chatType, EntityUid source, string entityName, string message, SpeechVerbPrototype? speech, LanguagePrototype? language, Color? colorOverride)
    {
        language ??= _language.GetLanguage(source);

        // Goobstation - Bolded Language Overrides begin
        if (language.SpeechOverride.BoldFontId != null && speech?.Bold == true)
            wrapId = "chat-manager-entity-say-bolded-language-wrap-message";
        // Goobstation end

        if (language.SpeechOverride.MessageWrapOverrides.TryGetValue(chatType, out var wrapOverride))
            wrapId = wrapOverride;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(source));

        var verbId = language.SpeechOverride.SpeechVerbOverrides is { } verbsOverride
            ? random.Pick(verbsOverride).ToString()
            : (speech is null ? "chat-speech-verb-default" : random.Pick(speech.SpeechVerbStrings));
        var color = DefaultSpeakColor;
        colorOverride ??= language.SpeechOverride.Color;
        if (colorOverride != null)
            color = Color.InterpolateBetween(color, colorOverride.Value, colorOverride.Value.A);
        var languageDisplay = language.IsVisibleLanguage
            ? Loc.GetString("chat-manager-language-prefix", ("language", language.ChatName))
            : "";

        // goob start - loudspeakers

        int? loudSpeakFont = null;

        var getLoudspeakerEv = new GetLoudspeakerEvent();
        RaiseLocalEvent(source, ref getLoudspeakerEv);

        if (getLoudspeakerEv.Loudspeakers != null)
            foreach (var loudspeaker in getLoudspeakerEv.Loudspeakers)
            {
                var loudSpeakerEv = new GetLoudspeakerDataEvent();
                RaiseLocalEvent(loudspeaker, ref loudSpeakerEv);

                if (loudSpeakerEv.IsActive && loudSpeakerEv.AffectChat)
                {
                    loudSpeakFont = loudSpeakerEv.FontSize;
                    break;
                }
            }

        // goob end

        // <Trauma> - allow source entity to replace font
        speech ??= GetSpeechVerb(source, message);
        var fontEv = new SpeechFontOverrideEvent(source, language.SpeechOverride.FontId ?? speech.FontId);
        RaiseLocalEvent(source, ref fontEv);
        // </Trauma>

        return Loc.GetString(wrapId,
            ("color", color),
            ("entityName", entityName),
            ("verb", Loc.GetString(verbId)),
            ("fontType", fontEv.Font), // Trauma - use Font from above
            ("fontSize", loudSpeakFont ?? language.SpeechOverride.FontSize ?? speech.FontSize),
            ("boldFontType", language.SpeechOverride.BoldFontId ?? language.SpeechOverride.FontId ?? speech.FontId), // Goob Edit - Custom Bold Fonts
            ("message", message),
            ("language", languageDisplay));
    }
}
