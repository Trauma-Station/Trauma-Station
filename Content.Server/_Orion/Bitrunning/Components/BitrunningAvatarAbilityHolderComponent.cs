// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Orion.Bitrunning.Components;

/// <summary>
/// Tracks actions granted to an avatar by bitrunning disks currently in its inventory tree.
/// </summary>
[RegisterComponent]
public sealed partial class BitrunningAvatarAbilityHolderComponent : Component
{
    public Dictionary<EntityUid, EntityUid?> ActionsByDisk = new();
}
