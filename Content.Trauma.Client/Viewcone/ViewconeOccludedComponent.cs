// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Viewcone;

/// <summary>
/// Component added to entities that are currently out of your vision cone.
/// Used to fade out the memory entity.
/// </summary>
[RegisterComponent]
public sealed partial class ViewconeOccludedComponent : Component
{
    /// <summary>
    /// When this entity was last in the client's vision cone, used for fading away.
    /// </summary>
    public TimeSpan LastSeen;
}
