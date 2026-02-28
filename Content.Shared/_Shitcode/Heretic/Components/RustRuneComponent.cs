// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Shitcode.Heretic.SpriteOverlay;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Goobstation.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RustRuneComponent : BaseSpriteOverlayComponent
{
    [DataField]
    public ProtoId<TagPrototype> DiagonalTag = "Diagonal";

    [DataField]
    public Vector2 DiagonalOffset = new(0.25f, -0.25f);

    [DataField]
    public string? SelectedRune;

    [DataField]
    public Vector2? SelectedOffset;

    [DataField]
    public List<string> RuneStates = new()
    {
        "small_rune_1",
        "small_rune_2",
        "small_rune_3",
        "small_rune_4",
        "small_rune_5",
        "small_rune_6",
        "small_rune_7",
        "small_rune_8",
        "small_rune_9",
        "small_rune_10",
        "small_rune_11",
        "small_rune_12",
    };

    public override Enum Key { get; set; } = RustRuneKey.Key;

    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Goobstation/Heretic/Effects/effects.rsi"), "small_rune_1");
}

public enum RustRuneKey : byte
{
    Key
}
