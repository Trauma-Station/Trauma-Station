using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.StatusIcon;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Speech;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    private string WrapRadioMessage(
        EntityUid source,
        RadioChannelPrototype channel,
        string name,
        string message,
        LanguagePrototype language,
        SpeechVerbPrototype speech,
        ProtoId<JobIconPrototype>? jobIcon,
        string? jobName = null)
    {
        // TODO: code duplication with ChatSystem.WrapMessage
        var languageColor = channel.Color;

        var wrapId = speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap";
        if (speech.Bold && language.SpeechOverride.BoldFontId != null)
            wrapId = "chat-radio-message-wrap-bolded-language";

        if (language.SpeechOverride.Color is { } colorOverride)
            languageColor = Color.InterpolateBetween(Color.White, colorOverride, colorOverride.A); // Changed first param to Color.White so it shows color correctly.

        var fontEv = new SpeechFontOverrideEvent(source, language.SpeechOverride.FontId ?? speech.FontId);
        RaiseLocalEvent(source, ref fontEv);

        var fontSizeEv = new SpeechFontSizeOverrideEvent(language.SpeechOverride.FontSize ?? speech.FontSize);
        RaiseLocalEvent(source, ref fontSizeEv);

        var nameString = jobIcon is null
            ? name
            : Loc.GetString("chat-radio-message-name-with-icon", ("jobIcon", jobIcon), ("jobName", jobName ?? ""), ("name", name));

        return Loc.GetString(wrapId,
            ("color", channel.Color),
            ("languageColor", languageColor),
            ("fontType", fontEv.Font),
            ("fontSize", fontSizeEv.FontSize),
            ("boldFontType", language.SpeechOverride.BoldFontId ?? language.SpeechOverride.FontId ?? speech.FontId),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", nameString),
            ("message", message));
    }
}
