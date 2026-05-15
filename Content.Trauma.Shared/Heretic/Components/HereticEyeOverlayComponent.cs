// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticEyeOverlayComponent : BaseSpriteOverlayComponent
{
    public override Enum Key { get; set; } = HereticEyeOverlayKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "heretic_eye_dripping");

    [DataField]
    public override Vector2 Offset { get; set; } = new(0f, 0.5f);
}

public enum HereticEyeOverlayKey : byte
{
    Key,
}
