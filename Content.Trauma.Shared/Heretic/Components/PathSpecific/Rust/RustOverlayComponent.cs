// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent]
public sealed partial class RustOverlayComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    [DataField]
    public string DiagonalState = "rust_diagonal";

    [DataField]
    public string OverlayState = "rust_default";

    public override Enum Key { get; set; } = RustOverlayKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "rune_default");
}

public enum RustOverlayKey : byte
{
    Key
}
