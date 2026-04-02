// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.CosmicCult.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class MonumentSpawnMarkComponent : Component
{
    /// <summary>
    /// The cultists that approved this mark. If enough approvals are granted, the actual monument will spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ApprovingCultists = [];

    [DataField, AutoNetworkedField]
    public int ApprovalsRequired;

    [DataField]
    public EntProtoId? MonumentSpawnIn = "MonumentCosmicCultSpawn";

}
