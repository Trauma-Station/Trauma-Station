using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent]
public sealed partial class LionhunterRifleAimMarkerComponent : BaseSpriteOverlayComponent
{
    public override Enum Key { get; set; } = LionhunterAimMarkerKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "sniper_zoom");
}

public enum LionhunterAimMarkerKey : byte
{
    Key,
}
