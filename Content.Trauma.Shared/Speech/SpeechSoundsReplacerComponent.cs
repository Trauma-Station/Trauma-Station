// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;

namespace Content.Trauma.Shared.Speech;

/// <summary>
/// Marks clothing that change wearer speech sound (for example - human talking like borg when wearing borg head (just example))
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SpeechSoundsReplacerComponent : Component
{
    /// <summary>
    /// A substitute sound
    /// </summary>
    [DataField]
    public ProtoId<SpeechSoundsPrototype>? SpeechSounds;

    /// <summary>
    /// Previous sound that returns when you unequip clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechSoundsPrototype>? PreviousSound;
}
