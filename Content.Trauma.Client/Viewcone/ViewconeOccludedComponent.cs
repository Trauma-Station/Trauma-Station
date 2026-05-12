// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Viewcone;

/// <summary>
/// Component added to entities that are currently out of your vision cone.
/// Used to track fake "freezing" their position, since it's just your memory rather than realtime.
/// </summary>
[RegisterComponent]
public sealed partial class ViewconeOccludedComponent : Component
{
    /// <summary>
    /// When this entity was last in the client's vision cone, used for fading away.
    /// </summary>
    public TimeSpan LastSeen;

    /// <summary>
    /// Map-local position it was last seen at, used to offset the sprite to prevent it from moving.
    /// Can't freeze the entity when drawing so here we are...
    /// </summary>
    public Vector2 LastPosition;

    /// <summary>
    /// The original sprite offset before overriding it to freeze the memory in place.
    /// Will be broken by animated offsets though...
    /// </summary>
    public Vector2 OriginalOffset;

    /// <summary>
    /// Local rotation it was last seen at, used to offset the sprite's angle.
    /// </summary>
    public Angle LastRotation;

    /// <summary>
    /// The original sprite rotation before overriding it.
    /// </summary>
    public Angle OriginalRotation;
}
