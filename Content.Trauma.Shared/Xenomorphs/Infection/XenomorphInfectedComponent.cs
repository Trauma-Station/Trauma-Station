// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Xenomorphs.Infection;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class XenomorphInfectedComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public Dictionary<int, ProtoId<InfectionIconPrototype>> InfectedIcons = new();

    [ViewVariables]
    public EntityUid Infection;

    [AutoNetworkedField, ViewVariables]
    public int GrowthStage;
}
