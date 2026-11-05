// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;

namespace Content.Trauma.Shared.InitPos;

/// <summary>
/// Stores ent's init location. Used with InitPosTeleport EntityEffect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InitPosComponent : Component
{
    // should i just use MapCoordinates and implement Teleport method that'll be using it?
    [DataField, AutoNetworkedField]
    public EntityCoordinates Coordinates;

    [DataField, AutoNetworkedField]
    public MapId Map;
}
