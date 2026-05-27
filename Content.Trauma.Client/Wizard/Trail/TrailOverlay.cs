// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Wizard.Projectile;
using Robust.Shared.Enums;
using Robust.Shared.Timing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Trauma.Client.Wizard.Trail;

public sealed class TrailOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private IEntityManager _ent;
    private IPrototypeManager _proto;
    private IGameTiming _timing;
    private SpriteSystem _sprite;
    private TransformSystem _transform;
    private EntityQuery<SpriteComponent> _spriteQuery;

    public TrailOverlay(IEntityManager entManager, IPrototypeManager protoMan, IGameTiming timing)
    {
        ZIndex = (int) DrawDepth.Overdoors;

        _ent = entManager;
        _proto = protoMan;
        _timing = timing;
        _sprite = _ent.System<SpriteSystem>();
        _transform = _ent.System<TransformSystem>();
        _spriteQuery = _ent.GetEntityQuery<SpriteComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var eye = args.Viewport.Eye;

        if (eye == null)
            return;

        var eyeRot = eye.Rotation;
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB;

        var query = _ent.EntityQueryEnumerator<TrailComponent, TransformComponent>();
        while (query.MoveNext(out _, out var trail, out var xform))
        {
            if (trail.TrailData.Count == 0)
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);

            if (trail.Shader != null && _proto.TryIndex<ShaderPrototype>(trail.Shader, out var shaderProto))
            {
                var shader = shaderProto.InstanceUnique();
                foreach (var (key, data) in trail.ShaderData)
                {
                    switch (data)
                    {
                        case GetShaderLocalPositionData:
                            shader.SetParameter(key, args.Viewport.WorldToLocal(position));
                            break;
                        case GetShaderFloatParam f:
                            if (float.TryParse(f.Param, out var fValue))
                                shader.SetParameter(key, fValue);
                            break;
                    }
                }
                handle.UseShader(shader);
            }
            else
                handle.UseShader(null);

            if (trail.RenderedEntity is { } uid)
            {
                Direction? direction = null;
                var rot = rotation;
                if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.Trail)
                {
                    var dirRot = rotation + eyeRot;
                    direction = dirRot.GetCardinalDir();
                }
                else if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.RenderedEntity)
                    rot = _transform.GetWorldRotation(uid);

                if (_spriteQuery.TryComp(uid, out var sprite))
                {
                    handle.SetTransform(Matrix3x2.Identity);
                    foreach (var data in trail.TrailData)
                    {
                        if (data.Color.A <= 0.01f || data.Scale <= 0.01f || data.MapId != args.MapId)
                            continue;

                        var worldPosition = data.Position;
                        if (!bounds.Contains(worldPosition))
                            continue;

                        if (trail.RenderedEntityRotationStrategy == RenderedEntityRotationStrategy.Particle)
                        {
                            rot = data.Angle;
                            direction = (rot + eyeRot).GetCardinalDir();
                        }

                        var originalColor = sprite.Color;
                        var originalScale = sprite.Scale;
                        var ent = (uid, sprite);
                        _sprite.SetColor(ent, data.Color);
                        _sprite.SetScale(ent, sprite.Scale * data.Scale);
                        _sprite.RenderSprite(ent, handle, eyeRot, rot, worldPosition, direction);
                        _sprite.SetColor(ent, originalColor);
                        _sprite.SetScale(ent, originalScale);
                    }
                }
                continue;
            }

            if (trail.Sprite == null)
            {
                handle.SetTransform(Matrix3x2.Identity);
                if (trail.ConnectLineToTrailEntity && xform.MapID == args.MapId)
                {
                    var start = trail.TrailData[^1].Position;
                    DrawTrailLine(start, position, trail.Color, trail.Scale, bounds, handle);
                }

                for (var i = 1; i < trail.TrailData.Count; i++)
                {
                    var data = trail.TrailData[i];
                    var prevData = trail.TrailData[i - 1];

                    if (data.MapId == args.MapId && prevData.MapId == args.MapId)
                        DrawTrailLine(prevData.Position, data.Position, data.Color, data.Scale, bounds, handle);
                }

                continue;
            }

            var textureSize = _sprite.Frame0(trail.Sprite).Size;
            var pos = -(Vector2) textureSize / 2f / EyeManager.PixelsPerMeter;
            foreach (var data in trail.TrailData)
            {
                if (data.Color.A <= 0.01f || data.Scale <= 0.01f || data.MapId != args.MapId)
                    continue;

                var worldPosition = data.Position;
                if (!bounds.Contains(worldPosition))
                    continue;

                var scaleMatrix = Matrix3x2.CreateScale(new Vector2(data.Scale, data.Scale));
                var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);

                var time = _timing.CurTime > data.SpawnTime ? _timing.CurTime - data.SpawnTime : TimeSpan.Zero;
                var texture = _sprite.GetFrame(trail.Sprite, time);

                handle.SetTransform(Matrix3x2.Multiply(scaleMatrix, worldMatrix));
                handle.DrawTexture(texture, pos, data.Angle, data.Color);
            }
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTrailLine(Vector2 start,
        Vector2 end,
        Color color,
        float scale,
        Box2 bounds,
        DrawingHandleWorld handle)
    {
        if (color.A <= 0.01f || scale <= 0.01f)
            return;

        if (!bounds.Contains(start) || !bounds.Contains(end))
            return;

        var halfScale = scale * 0.5f;
        var direction = end - start;
        var angle = direction.ToAngle();
        var box = new Box2(start - new Vector2(0f, halfScale),
            start + new Vector2(direction.Length(), halfScale));
        var boxRotated = new Box2Rotated(box, angle, start);
        handle.DrawRect(boxRotated, color);
    }
}
