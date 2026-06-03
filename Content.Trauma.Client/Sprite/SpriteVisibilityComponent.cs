// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Sprite;

/// <summary>
/// Controls sprite visibility, used to avoid conflicts for different systems/overlays modifying alpha
/// </summary>
[RegisterComponent]
public sealed partial class SpriteVisibilityComponent : Component
{
    /// <summary>
    /// Source key -> alpha value [0, 1)
    /// Final alpha is calculated by multiplying the values
    /// If final alpha is 0, sprite.Visible is set to false
    /// </summary>
    [DataField]
    public Dictionary<string, float> VisibilityModifiers = new();
}
