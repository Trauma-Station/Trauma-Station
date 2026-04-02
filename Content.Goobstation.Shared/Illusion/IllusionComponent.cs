// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Illusion;

[RegisterComponent, NetworkedComponent]
public sealed partial class IllusionComponent : Component
{
    [DataField]
    public LocId DeathMessage = "illusion-comp-death-message";
}
