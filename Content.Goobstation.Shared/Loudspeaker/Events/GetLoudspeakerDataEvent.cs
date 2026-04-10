// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;

namespace Content.Goobstation.Shared.Loudspeaker.Events;

[ByRefEvent]
public record struct GetLoudspeakerDataEvent(
    bool IsActive = false,
    int? FontSize = null,
    bool AffectRadio = false,
    bool AffectChat = false,
    ProtoId<SpeechSoundsPrototype>? SpeechSounds = null);
