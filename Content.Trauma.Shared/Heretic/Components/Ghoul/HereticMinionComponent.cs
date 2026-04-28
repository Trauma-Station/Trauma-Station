// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticMinionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? BoundHeretic;

    [DataField, AutoNetworkedField]
    public int MinionId;

    [DataField]
    public EntityUid? CreationRitual;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<FactionIconPrototype> MasterIcon { get; set; } = "GhoulHereticMaster";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<FactionIconPrototype> GhoulIcon { get; set; } = "GhoulFaction";
}
