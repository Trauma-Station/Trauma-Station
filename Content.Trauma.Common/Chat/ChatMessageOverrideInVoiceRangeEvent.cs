// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Trauma.Common.Language;

namespace Content.Trauma.Common.Chat;

[ByRefEvent]
public record struct ChatMessageOverrideInVoiceRange(EntityUid Source, string Name, ProtoId<LanguagePrototype> Language, SpeechVerbPrototype? Speech, Color? Color, string Message, string WrappedMessage, bool Cancelled = false)
{
    public void Cancel()
    {
        Cancelled = true;
    }
}
