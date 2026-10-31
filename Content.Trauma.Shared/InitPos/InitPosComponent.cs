// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.InitPos;

/// <summary>
/// Stores ent's init location and grid.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InitPosComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Coordinates = default;
}
