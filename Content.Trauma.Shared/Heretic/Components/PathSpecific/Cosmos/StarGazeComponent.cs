// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarGazeComponent : Component
{
    [DataField]
    public Vector2 Slowdown = new(0.1f, 0.1f);

    [DataField]
    public int LastStage = -1;

    [DataField]
    public float ScreamProb = 0.05f;

    [DataField]
    public float MaxThrowLength = 0.01f;

    [DataField]
    public float ThrowSpeed = 1f;

    [DataField]
    public SoundSpecifier ObliterateSound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/supermatter.ogg");

    [DataField]
    public EntProtoId AshProto = "Ash";

    [DataField]
    public float GravityPullSizeModifier = 2f;

    [DataField]
    public SpriteSpecifier Beam1 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "beam1");

    [DataField]
    public SpriteSpecifier Beam2 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "beam2");

    [DataField]
    public SpriteSpecifier Beam3 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "beam3");

    [DataField]
    public SpriteSpecifier Start1 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects96x96.rsi"), "start1");

    [DataField]
    public SpriteSpecifier End1 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "end1");

    [DataField]
    public SpriteSpecifier Start2 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects96x96.rsi"), "start2");

    [DataField]
    public SpriteSpecifier End2 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "end2");

    [DataField]
    public SpriteSpecifier Start3 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects96x96.rsi"), "start3");

    [DataField]
    public SpriteSpecifier End3 =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_Goobstation/Heretic/Effects/effects.rsi"), "end3");
}
