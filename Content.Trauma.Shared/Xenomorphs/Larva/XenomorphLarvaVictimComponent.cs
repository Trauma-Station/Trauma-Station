// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Xenomorphs.Larva;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class XenomorphLarvaVictimComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<StatusIconPrototype>? InfectedIcon;
}
