using Content.Shared.Radio;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.StatusIcon;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Speech;
using Robust.Shared.Prototypes;
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
        return WrapMessage(wrapId, InGameICChatType.Speak, null, source, name, message, speech, language, colorOverride, null, null);
    }

    /// <summary>
    ///     Wraps a message whispered by the specified entity into an "x whispers y" string.
    /// </summary>
    public string WrapWhisperMessage(EntityUid source, LocId defaultWrap, string entityName, string message, LanguagePrototype? language = null, Color? colorOverride = null)
    {
        return WrapMessage(defaultWrap, InGameICChatType.Whisper, null, source, entityName, message, null, language, colorOverride, null, null);
    }

    /// <summary>
    ///     Wraps a message sent by the specified entity into the specified wrap string.
    /// </summary>
    public string WrapMessage(LocId? wrapId, InGameICChatType? chatType, RadioChannelPrototype? channel, EntityUid source, string entityName, string message, SpeechVerbPrototype? speech, LanguagePrototype? language, Color? colorOverride, ProtoId<JobIconPrototype>? jobIcon, string? jobName)
    {
        language ??= _language.GetLanguage(source);

        bool isRadio = channel is { };

        if (language.SpeechOverride.BoldFontId != null && speech?.Bold == true)
            wrapId = "chat-manager-entity-say-bolded-language-wrap-message";

        var color = DefaultSpeakColor;
        colorOverride ??= language.SpeechOverride.Color;

        if (colorOverride is { })
            color = Color.InterpolateBetween(color, colorOverride.Value, colorOverride.Value.A);

        var fonts = GetFont(source, speech, language, message);
        speech ??= GetSpeechVerb(source, message);

        LocId finalWrapId = wrapId ?? (isRadio
            ? (speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap")
            : (speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message"));

        if (speech.Bold && language.SpeechOverride.BoldFontId is { })
            finalWrapId = isRadio ? "chat-radio-message-wrap-bolded-language" : "chat-manager-entity-say-bolded-language-wrap-message";

        if (!isRadio && chatType is { } chat && language.SpeechOverride.MessageWrapOverrides.TryGetValue(chat, out var wrapOverride))
            finalWrapId = wrapOverride;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(source));

        var baseColor = channel is { } ? channel?.Color : DefaultSpeakColor;

        colorOverride ??= language.SpeechOverride.Color;
        if (colorOverride is { } && baseColor is { } colorBase)
        {
            var blendSource = isRadio ? Color.White : colorBase;
            baseColor = Color.InterpolateBetween(blendSource, colorOverride.Value, colorOverride.Value.A);
        }

        var boldFontType = language.SpeechOverride.BoldFontId ?? language.SpeechOverride.FontId ?? speech.FontId;

        var nameString = jobIcon is not { }
            ? entityName
            : Loc.GetString("chat-radio-message-name-with-icon", ("jobIcon", jobIcon), ("jobName", jobName ?? ""), ("name", entityName));

        return Loc.GetString(finalWrapId,
            ("color", channel is { } ? channel.Color : color),
            ("entityName", entityName),
            ("languageColor", baseColor ?? color),
            ("fontType", fonts.FontType),
            ("fontSize", fonts.FontSize),
            ("boldFontType", boldFontType),
            ("verb", fonts.VerbId),
            ("channel", channel is { } ? $"\\[{channel.LocalizedName}\\]" : string.Empty),
            ("name", nameString),
            ("message", message));
    }

    public (string VerbId, string FontType, string FontSize) GetFont(EntityUid source, SpeechVerbPrototype? speech, LanguagePrototype language, string message)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(source));

        var verbId = language.SpeechOverride.SpeechVerbOverrides is { } verbsOverride
            ? random.Pick(verbsOverride).ToString()
            : (speech is null ? "chat-speech-verb-default" : random.Pick(speech.SpeechVerbStrings));

        speech ??= GetSpeechVerb(source, message);

        var fontEv = new SpeechFontOverrideEvent(source, language.SpeechOverride.FontId ?? speech.FontId);
        RaiseLocalEvent(source, ref fontEv);

        var fontSizeEv = new SpeechFontSizeOverrideEvent(language.SpeechOverride.FontSize ?? speech.FontSize);
        RaiseLocalEvent(source, ref fontSizeEv);

        return (Loc.GetString(verbId), fontEv.Font, fontSizeEv.FontSize.ToString());
    }
}
