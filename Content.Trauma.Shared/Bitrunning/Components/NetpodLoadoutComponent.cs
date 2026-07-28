// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NetpodLoadoutComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<StartingGearPrototype>? PreferredLoadout = "BitrunnerAvatarGear";

    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, ProtoId<StartingGearPrototype>> PreferredLoadoutSpecie = new();

    [DataField]
    public List<ProtoId<StartingGearPrototype>> AllowedLoadout = new();

    [DataField]
    public Dictionary<ProtoId<SpeciesPrototype>, List<ProtoId<StartingGearPrototype>>> AllowedLoadoutSpecie = new();
}
