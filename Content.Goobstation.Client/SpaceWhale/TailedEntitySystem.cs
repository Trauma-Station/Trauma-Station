// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;

namespace Content.Goobstation.Client.SpaceWhale;

public sealed partial class TailedEntitySystem : SharedTailedEntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    [SubscribeLocalEvent]
    private void OnAfterAutoHandleState(Entity<TailedEntityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_spriteQuery.TryGetComponent(ent.Owner, out var sprite))
            sprite.RenderOrder = (uint) ent.Comp.TailSegments.Count + 5;
    }

    [SubscribeLocalEvent]
    private void OnSegmentAfterAutoHandleState(Entity<TailedEntitySegmentComponent> ent,
        ref AfterAutoHandleStateEvent args)
    {
        if (!_spriteQuery.TryGetComponent(ent.Owner, out var sprite))
            return;

        sprite.RenderOrder = (uint) (ent.Comp.SegmentCount - ent.Comp.Order + 5);

        if (ent.Comp.SegmentSpriteState is not { } segmentState || ent.Comp.TailSpriteState is not { } tailState)
            return;

        _sprite.LayerSetRsiState((ent, sprite),
            TailedEntitySegmentLayer.Base,
            ent.Comp.Order == ent.Comp.SegmentCount - 1 ? tailState : segmentState);
    }

    [SubscribeLocalEvent]
    private void OnMove(Entity<TailedEntityComponent> ent, ref MoveEvent args)
    {
        if (args.OldPosition == args.NewPosition && args.OldRotation == args.NewRotation ||
            TerminatingOrDeleted(args.Entity) || ent.Comp.TailSegments.Count == 0)
            return;

        UpdateTailPositions((ent, ent.Comp, args.Entity.Comp1));
    }
}
