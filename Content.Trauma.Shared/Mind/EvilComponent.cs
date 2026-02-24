// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Mind;

/// <summary>
/// Marker component for a mob or mind entity that is always evil.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EvilComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
