// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Gives the mutated mob intrinsic radio channels.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RadioMutationSystem))]
[AutoGenerateComponentState]
public sealed partial class RadioMutationComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    /// Active radio channels that were added and not already present.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> AddedActive = new();

    /// <summary>
    /// Transmitter channels that were added and not already present.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<RadioChannelPrototype>> AddedTransmitters = new();
}
