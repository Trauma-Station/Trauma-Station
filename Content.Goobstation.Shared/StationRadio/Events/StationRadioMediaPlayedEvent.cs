using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.StationRadio.Events;

[ByRefEvent]
public record struct StationRadioMediaPlayedEvent(SoundPathSpecifier MediaPlayed);
