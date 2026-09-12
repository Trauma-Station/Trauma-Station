// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.Magic.Demonologist.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DemonologistApprenticeComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "DemonologistApprenticeFaction";
}
