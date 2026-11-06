// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Teleportation;

/// <summary>
/// Stores location of RandomTeleport EntityEffect.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OneWayPortalComponent : Component
{
    [DataField]
    public SoundSpecifier? TeleportSound = new SoundPathSpecifier("/Audio/Items/hiss.ogg");

    [DataField, AutoNetworkedField]
    public EntityCoordinates Destination;
}
