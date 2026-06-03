namespace Content.Trauma.Common.Sprite;

/// <summary>
/// Modifies sprite visibility. Used to avoid conflicts with multiple different systems/overlays changing visibility
/// </summary>
/// <param name="Key">Key for visibility source</param>
/// <param name="Alpha">Sprite color alpha,
/// use value greator or equal to 1 to remove visibility modifier
/// and less or equal to 0 to set sprite.Visible to false</param>
[ByRefEvent]
public readonly record struct UpdateSpriteVisibilityEvent(string Key, float Alpha);
