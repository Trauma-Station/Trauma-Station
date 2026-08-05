// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.SpeechPro;

[Serializable, NetSerializable]
public sealed class SpeechProUiMessage(ProtoId<SpeechProPhrasePrototype> phrase) : BoundUserInterfaceMessage
{
    public readonly ProtoId<SpeechProPhrasePrototype> Phrase = phrase;
}
