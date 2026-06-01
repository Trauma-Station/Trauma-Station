using Content.Shared.Speech;

namespace Content.Trauma.Common.Speech;

/// <summary>
/// Event raised on a speech source to allow replacing the font used.
/// </summary>
[ByRefEvent]
public record struct SpeechFontSizeOverrideEvent(int FontSize, bool IsActive = false, bool AffectRadio = false, bool AffectChat = false, ProtoId<SpeechSoundsPrototype>? SpeechSounds = null);
