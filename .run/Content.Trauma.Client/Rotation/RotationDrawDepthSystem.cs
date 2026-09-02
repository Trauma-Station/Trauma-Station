// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Client.Rotation;

public sealed partial class RotationDrawDepthSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<RotationDrawDepthComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite, out var xform))
        {
            // TODO: this needs to support rotated viewports eventually
            var dir = xform.LocalRotation.GetCardinalDir();
            _sprite.SetDrawDepth((uid, sprite), dir switch
            {
                Direction.South => comp.SouthDrawDepth,
                _ => comp.DefaultDrawDepth
            });
        }
    }
}
