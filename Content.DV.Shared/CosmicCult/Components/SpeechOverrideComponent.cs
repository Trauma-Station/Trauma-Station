using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Speech;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.CosmicCult.Components;

/// <summary>
/// Component for overriding an entity's vocal and speech sounds through equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeechOverrideComponent : Component
{
    /// <summary>
    /// Emote sounds to assign to the entity equipping this item.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? EmoteIDs = null;

    /// <summary>
    /// Entity's original emote sounds to use when the item is unequipped.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? EmoteStoredIDs = null;

    [DataField(required: true)]
    public ProtoId<SpeechSoundsPrototype>? SpeechIDs = null;

    [DataField]
    [AutoNetworkedField]
    public ProtoId<SpeechSoundsPrototype>? SpeechStoredIDs = null;

}
