// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementIgnoreGravityClothingComponent : Component
{
    [DataField]
    public bool Weightless;
}
