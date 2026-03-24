// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Shitcode.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VoidCurseComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public float Lifetime = 5f; // 8s on 1 stack, 20s on max stack

    [DataField]
    public float MaxLifetime = 5f;

    [DataField]
    public float LifetimeIncreasePerLevel = 3f;

    [DataField, AutoNetworkedField]
    public float Stacks;

    [DataField]
    public float MaxStacks = 5f;

    public float Timer = 1f;

    [DataField]
    public string OverlayStateNormal = "void_chill_partial";

    [DataField]
    public string OverlayStateMax = "void_chill_oh_fuck";

    public override Enum Key { get; set; } = VoidCurseKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/void_overlay.rsi"), "void_chill_partial");
}

public enum VoidCurseKey : byte
{
    Key,
}
