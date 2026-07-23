// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;

namespace Content.Trauma.Shared.CosmicCult.Components;

/// <summary>
/// Component for overriding a mob's emote and speech sounds while this clothing worn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpeechOverrideComponent : Component
{
    /// <summary>
    /// Emote sounds to assign to the entity equipping this item.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds;

    /// <summary>
    /// Entity's original emote sounds to use when the item is unequipped.
    /// </summary>
    [DataField]
    public ProtoId<EmoteSoundsPrototype>? OldEmoteSounds;

    [DataField(required: true)]
    public ProtoId<SpeechSoundsPrototype>? SpeechSounds = null;

    [DataField]
    public ProtoId<SpeechSoundsPrototype>? OldSpeechSounds = null;
}
