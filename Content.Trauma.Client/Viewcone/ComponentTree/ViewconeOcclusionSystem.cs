// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Trauma.Client.Viewcone.ComponentTree;

/// <summary>
/// Handles gathering sprites to modify alpha in the viewcone overlays
/// </summary>
public sealed class ViewconeOcclusionSystem : ComponentTreeSystem<ViewconeOccludableTreeComponent, ViewconeOccludableComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<ViewconeOccludableComponent> entry, Vector2 pos, Angle rot)
    {
        return _sprite.CalculateBounds((entry.Uid, _spriteQuery.Comp(entry.Uid)), pos, rot, default).CalcBoundingBox();
    }
}
