// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Trauma.Common.Language;

namespace Content.Trauma.Common.Chat;

/// <summary>
/// Event raised on an entity in voice range of a chat message to be either modified or cancelled.
/// </summary>
[ByRefEvent]
public record struct ChatMessageOverrideInVoiceRangeEvent(EntityUid Source, string Name, ProtoId<LanguagePrototype> Language, SpeechVerbPrototype? Speech, Color? Color, string Message, string WrappedMessage, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
