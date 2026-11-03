// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Xenomorphs.Infection;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class XenomorphInfectedComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, ProtoId<StatusIconPrototype>> InfectedIcons = new();

    [DataField]
    public EntityUid Infection;

    [DataField, AutoNetworkedField]
    public int GrowthStage;
}
