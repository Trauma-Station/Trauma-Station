// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Bitrunning.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AvatarNavRelayComponent : Component
{
    /// <summary>
    /// Paired relay entity used for camera/view subscription redirection between avatar and netpod.
    /// Set when an avatar is spawned/connected, and on disconnect active viewers are removed before this link is cleared.
    /// If null, callers should publish ActiveCamera = null instead of falling back to a stale netpod target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RelayEntity;
}
