// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticAuraComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "heretic_aura");

    public override Enum Key { get; set; } = HereticAuraKey.Key;

    public override bool Unshaded { get; set; } = false;
}

public enum HereticAuraKey : byte
{
    Key,
}
