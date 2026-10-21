// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Enums;

namespace Content.Trauma.Client.Heretic;

public sealed partial class ItemOverlayOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpaceBelowWorld;

    [Dependency] private IEntityManager _entMan = default!;

    private readonly TransformSystem _xform;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;

    public ItemOverlayOverlay()
    {
        IoCManager.InjectDependencies(this);

        _xform = _entMan.System<TransformSystem>();

        _spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;

        if (viewport.Eye is not { } eye)
            return;

        var eyeRot = eye.Rotation;
        var handle = args.ScreenHandle;
        var worldToLocalMatrix = viewport.GetWorldToLocalMatrix();

        var query = _entMan.EntityQueryEnumerator<ItemOverlayComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var overlay, out var sprite, out var xform))
        {
            if (!sprite.Visible || overlay.RT is not { } rt)
                continue;

            var ev = new GetItemOverlayEntityEvent();
            _entMan.EventBus.RaiseLocalEvent(uid, ref ev);
            if (ev.Result is not { } result || !_spriteQuery.TryComp(result, out var resultSprite))
                continue;

            var rot = _xform.GetWorldRotation(xform);

            handle.RenderInRenderTarget(rt, () =>
            {
                handle.DrawEntity(result, overlay.RenderTargetSize * 0.5f, Vector2.One, Angle.Zero, sprite: resultSprite, xformSystem: _xform);
            }, Color.Transparent);
        }
    }
}
