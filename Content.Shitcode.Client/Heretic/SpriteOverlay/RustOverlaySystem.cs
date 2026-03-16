// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared.Tag;
using Robust.Client.GameObjects;

namespace Content.Client._Shitcode.Heretic.SpriteOverlay;

public sealed class RustOverlaySystem : SpriteOverlaySystem<RustOverlayComponent>
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RustOverlayComponent, IconSmoothCornersInitializedEvent>(OnIconSmoothInit);
    }

    private void OnIconSmoothInit(Entity<RustOverlayComponent> ent, ref IconSmoothCornersInitializedEvent args)
    {
        RemoveOverlay(ent.Owner, ent.Comp);
        AddOverlay(ent.Owner, ent.Comp);
    }

    protected override void UpdateOverlayLayer(Entity<SpriteComponent> ent,
        RustOverlayComponent comp,
        int layer,
        EntityUid? source = null)
    {
        base.UpdateOverlayLayer(ent, comp, layer, source);

        var diagonal = _tag.HasTag(ent, comp.DiagonalTag);
        var state = diagonal ? comp.DiagonalState : comp.OverlayState;

        Sprite.LayerSetRsiState(ent.AsNullable(), layer, state);
    }
}
