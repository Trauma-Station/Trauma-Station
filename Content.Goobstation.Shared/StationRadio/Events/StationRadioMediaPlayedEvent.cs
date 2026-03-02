// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.StationRadio.Events;

[ByRefEvent]
public record struct StationRadioMediaPlayedEvent(SoundPathSpecifier MediaPlayed);
