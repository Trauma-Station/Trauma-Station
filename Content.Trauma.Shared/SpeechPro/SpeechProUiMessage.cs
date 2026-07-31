// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.SpeechPro;

[Serializable, NetSerializable]
public sealed class SpeechProUiMessage : BoundUserInterfaceMessage
{
    public readonly byte Phrase;

    public SpeechProUiMessage(byte phrase)
    {
        Phrase = phrase;
    }
}
