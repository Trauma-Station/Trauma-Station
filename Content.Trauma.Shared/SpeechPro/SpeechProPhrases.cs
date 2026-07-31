// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;

namespace Content.Trauma.Shared.SpeechPro;

public enum SpeechProPhraseSection : byte
{
    Basic,
    Needs,
    Tourism,
    Alerts,
}

public readonly record struct SpeechProPhrase(string Id, SpeechProPhraseSection Section)
{
    public string ButtonLocId => $"speech-pro-button-{Id}";
    public string MessageLocId => $"speech-pro-phrase-{Id}";
}

public static class SpeechProPhrases
{
    public static IReadOnlyList<SpeechProPhrase> All { get; } = new SpeechProPhrase[]
    {
        new("greeting", SpeechProPhraseSection.Basic),
        new("farewell", SpeechProPhraseSection.Basic),
        new("affirmation", SpeechProPhraseSection.Basic),
        new("rejection", SpeechProPhraseSection.Basic),
        new("gratitude", SpeechProPhraseSection.Basic),
        new("apology", SpeechProPhraseSection.Basic),
        new("please-repeat", SpeechProPhraseSection.Basic),
        new("slower", SpeechProPhraseSection.Basic),
        new("explanation", SpeechProPhraseSection.Basic),

        new("permission", SpeechProPhraseSection.Tourism),
        new("trade", SpeechProPhraseSection.Tourism),
        new("observe", SpeechProPhraseSection.Tourism),
        new("photograph", SpeechProPhraseSection.Tourism),
        new("follow", SpeechProPhraseSection.Tourism),
        new("where-shuttle", SpeechProPhraseSection.Tourism),

        new("congratulations", SpeechProPhraseSection.Alerts),
        new("applaud", SpeechProPhraseSection.Alerts),
        new("harmless", SpeechProPhraseSection.Alerts),
        new("dangerous", SpeechProPhraseSection.Alerts),
        new("panic", SpeechProPhraseSection.Alerts),
        new("hungry", SpeechProPhraseSection.Alerts),
        new("thirsty", SpeechProPhraseSection.Alerts),
        new("assistance", SpeechProPhraseSection.Alerts),
        new("confusion", SpeechProPhraseSection.Alerts),
        new("wait", SpeechProPhraseSection.Alerts),
        new("lost", SpeechProPhraseSection.Alerts),
    };
}
