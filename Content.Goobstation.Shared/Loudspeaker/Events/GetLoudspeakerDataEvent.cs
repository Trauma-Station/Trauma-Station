// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Loudspeaker.Events;

[ByRefEvent]
public record struct GetLoudspeakerDataEvent(
    bool IsActive = false,
    int? FontSize = null,
    bool AffectRadio = false,
    bool AffectChat = false,
    ProtoId<SpeechSoundsPrototype>? SpeechSounds = null);
