// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Trauma.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.Weapons.Ranged;

/// <summary>
/// Draws bullet holes on objects
/// </summary>
public sealed class BulletHoleOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan    = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    private readonly TransformSystem _xform;

    private const string RsiPath  = "/Textures/_RMC14/Effects/bulletholes.rsi";
    private const string RsiState = "bullethole";
    private static readonly Vector2 DrawSize = Vector2.One;
    private static readonly Box2 HoleBox = Box2.CenteredAround(Vector2.Zero, DrawSize);

    private Texture? _texture;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public BulletHoleOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xform = _entMan.System<TransformSystem>();
        ZIndex = -2; // Renderer it under every other overlay
    }

    private Texture? GetTexture()
    {
        if (_texture != null)
            return _texture;

        var rsi = _resources.GetResource<RSIResource>(new ResPath(RsiPath)).RSI;
        if (rsi.TryGetState(RsiState, out var state))
            _texture = state.GetFrames(RsiDirection.South)[0];
        return _texture;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (GetTexture() is not {} texture)
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var query  = _entMan.AllEntityQueryEnumerator<BulletHoleComponent>();
        var expandedBounds = bounds.Enlarged(2f);

        while (query.MoveNext(out var uid, out var holes))
        {
            if (holes.HolePositions.Count == 0 || !_entMan.TryGetComponent<TransformComponent>(uid, out var xform))
                continue;

            var worldPos = _xform.GetWorldPosition(uid);

            if (!expandedBounds.Contains(worldPos))
                continue;

            var gridUid = xform.GridUid;
            var gridRot = gridUid != null
                ? _xform.GetWorldRotation(gridUid.Value)
                : Angle.Zero;

            var bulletRot = Matrix3x2.CreateRotation((float) gridRot);
            foreach (var localOffset in holes.HolePositions)
            {
                var worldOffset = Vector2.Transform(localOffset, bulletRot);
                var center = worldPos + worldOffset;

                handle.SetTransform(
                    bulletRot *
                    Matrix3x2.CreateTranslation(center));

                handle.DrawTextureRect(texture, HoleBox);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
