// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Sprite;

public abstract partial class CommonSpriteVisibilitySystem : EntitySystem
{
    /// <summary>
    /// Modifies sprite visibility. Used to avoid conflicts with multiple different systems/overlays changing visibility
    /// </summary>
    /// <param name="uid">Target entity</param>
    /// <param name="key">Key for visibility source</param>
    /// <param name="alpha">Sprite color alpha,
    /// use value greater or equal to 1 to remove visibility modifier
    /// and less or equal to 0 to set sprite.Visible to false</param>
    public abstract void UpdateVisibilityModifiers(EntityUid uid, string key, float alpha);

    /// <summary>
    /// Modifies sprite visibility to be opaque or invisible for a key.
    /// </summary>
    public void UpdateVisibilityModifiers(EntityUid uid, string key, bool visible)
    {
        UpdateVisibilityModifiers(uid, key, visible ? 1f : 0f);
    }
}
