// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticCosmicMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? CosmicDiamondUid;

    [DataField, AutoNetworkedField]
    public int PathStage;

    [DataField]
    public EntProtoId CosmicDiamond = "EffectCosmicDiamond";

    [DataField]
    public EntProtoId CosmicCloud = "EffectCosmicCloud";
}
