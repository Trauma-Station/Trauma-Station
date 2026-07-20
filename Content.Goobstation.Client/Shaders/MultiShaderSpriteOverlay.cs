// SPDX-License-Identifier: AGPL-3.0-or-later

// TODO PR this to engine, otherwise I have no clue how to fix lighting not applying properly

using System.Linq;
using Robust.Shared.Enums;

namespace Content.Goobstation.Client.Shaders;

public sealed partial class MultiShaderSpriteOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IClyde _clyde = default!;

    private readonly TransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly ContainerSystem _container;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public MultiShaderSpriteOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transform = _entMan.System<TransformSystem>();
        _sprite = _entMan.System<SpriteSystem>();
        _container = _entMan.System<ContainerSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;

        if (viewport.Eye is not { } eye)
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(2f);
        var mapId = args.MapId;

        var localMatrix = viewport.GetWorldToLocalMatrix();

        var query = _entMan.EntityQueryEnumerator<MultiShaderSpriteComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var multi, out var sprite, out var xform))
        {
            if (multi.PostShaders.Count == 0 || !sprite.Visible || xform.MapID != mapId ||
                _container.IsEntityInContainer(uid))
                continue;

            var (pos, rot) = _transform.GetWorldPositionRotation(xform);

            if (!bounds.Contains(pos))
                continue;

            var multipleDirs =
                sprite.AllLayers.Any(x => x is SpriteComponent.Layer l && _sprite.LayerGetDirectionCount(l) > 1);

            var rotAdjusted = multipleDirs && !sprite.NoRotation ? -eye.Rotation : Angle.Zero;
            var finalRot = rot + rotAdjusted;
            var spriteBB = _sprite.CalculateBounds((uid, sprite), pos, rot, eye.Rotation);
            var screenBB = localMatrix.TransformBox(spriteBB.Box);
            var screenSpriteSize = (Vector2i) screenBB.Size.Rounded();

            if (screenSpriteSize.X == 0 || screenSpriteSize.Y == 0)
                continue;

            if (screenSpriteSize.X % 2 != 0)
                screenSpriteSize.X++;
            if (screenSpriteSize.Y % 2 != 0)
                screenSpriteSize.Y++;

            if (multi.RenderTarget is not { } target)
            {
                target = _clyde.CreateRenderTarget(screenSpriteSize,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                    name: $"multi_shader_{uid}");

                multi.RenderTarget = target;
            }
            else if (target.Size != screenSpriteSize)
            {
                target.Dispose();

                target = _clyde.CreateRenderTarget(screenSpriteSize,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                    name: $"multi_shader_{uid}");

                multi.RenderTarget = target;
            }

            var quad = Box2.FromDimensions(Vector2.Zero, screenSpriteSize).Scale(new Vector2(1f, -1f));

            var postHandle = new DrawingHandleMultiShader(Texture.White, handle);

            var rotation = multipleDirs ? rot : -eye.Rotation;

            postHandle.RenderInRenderTarget(target,
                () =>
                {
                    var position = target.LocalToWorld(eye, (Vector2) screenSpriteSize * 0.5f, viewport.RenderScale);
                    var angle = rotation + eye.Rotation;
                    angle = angle.Reduced().FlipPositive();

                    var cardinal = Angle.Zero;

                    if (sprite is { NoRotation: false, SnapCardinals: true })
                        cardinal = angle.RoundToCardinalAngle();

                    var entityMatrix = Matrix3Helpers.CreateTransform(position,
                        sprite.NoRotation ? -eye.Rotation : rotation - cardinal);
                    Matrix3x2.Invert(entityMatrix, out var invEntityMatrix);

                    var invMatrix = target.GetWorldToLocalMatrix(eye, viewport.RenderScale);

                    Matrix3x2.Invert(sprite.LocalMatrix, out var invSpriteMatrix);

                    var theta = (float) eye.Rotation.Theta;
                    var absSin = MathF.Abs(MathF.Sin(theta));
                    var absCos = MathF.Abs(MathF.Cos(theta));
                    var s = sprite.Scale;
                    var scale = new Vector2(absCos * s.X + absSin * s.Y, absSin * s.X + absCos * s.Y);

                    var scaleMatrix = Matrix3Helpers.CreateScale(scale);

                    postHandle.InvMatrix = invEntityMatrix * invSpriteMatrix * scaleMatrix * entityMatrix * invMatrix;

                    _sprite.RenderSprite((uid, sprite), postHandle, eye.Rotation, rotation, position);
                    postHandle.InvMatrix = Matrix3x2.Identity;

                    postHandle.SetTransform(Matrix3x2.Identity);

                    foreach (var (protoId, data) in multi.PostShaders.OrderBy(x => x.Value.RenderOrder))
                    {
                        var proto = _proto.Index<ShaderPrototype>(protoId);
                        if (!multi.CurrentShaders.TryGetValue(protoId, out var shader))
                        {
                            shader = data.Mutable ? proto.InstanceUnique() : proto.Instance();
                            multi.CurrentShaders[protoId] = shader;
                        }

                        if (data.RaiseShaderEvent)
                        {
                            var ev = new BeforePostMultiShaderRenderEvent(proto.ID, shader, sprite, viewport);
                            _entMan.EventBus.RaiseLocalEvent(uid, ref ev);
                        }

                        postHandle.UseShader(shader);
                        postHandle.DrawTextureRectRegion(target.Texture, quad, data.Color);
                    }

                    if (sprite.PostShader == null)
                        return;

                    postHandle.UseShader(sprite.PostShader);
                    if (sprite.RaiseShaderEvent)
                        _entMan.EventBus.RaiseLocalEvent(uid, new BeforePostShaderRenderEvent(sprite, viewport));
                    postHandle.DrawTextureRectRegion(target.Texture, quad);
                },
                Color.Transparent);

            handle.UseShader(null);
            var mat = Matrix3x2.CreateTranslation(pos + (rotation - rot).RotateVec(sprite.Offset) - spriteBB.Center);
            handle.SetTransform(mat);
            handle.DrawTextureRectRegion(target.Texture, spriteBB);
            handle.SetTransform(Matrix3x2.Identity);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
