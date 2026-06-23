// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Orion.Bitrunning.Systems;

namespace Content.Server._Orion.Bitrunning.Components;

[RegisterComponent]
[Access(typeof(QuantumServerSystem), typeof(NetpodSystem))]
public sealed partial class AvatarNavRelayComponent : Component
{
    /// <summary>
    /// Paired relay entity used for camera/view subscription redirection between avatar and netpod.
    /// Set when an avatar is spawned/connected, and on disconnect active viewers are removed before this link is cleared.
    /// If null, callers should publish ActiveCamera = null instead of falling back to a stale netpod target.
    /// </summary>
    public EntityUid? RelayEntity;
}
