// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.StatusIcon;

namespace Content.Trauma.Shared.Xenomorphs.Larva;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class XenomorphLarvaVictimComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public ProtoId<InfectionIconPrototype>? InfectedIcon;
}
