// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Weapons.Ranged;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using System.Runtime.InteropServices;

namespace Content.Trauma.Client.Weapons.Ranged;

/// <summary>
/// Draws bullet holes on objects
/// </summary>
public sealed partial class BulletHoleOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan    = default!;
    [Dependency] private IResourceCache _resources = default!;

    private readonly TransformSystem _xform;

    private const string RsiPath  = "/Textures/_RMC14/Effects/bulletholes.rsi";
    private const string RsiState = "bullethole";
    private static readonly Vector2 DrawSize = Vector2.One;
    private static readonly Box2 HoleBox = Box2.CenteredAround(Vector2.Zero, DrawSize);

    private Texture? _texture;
    private List<WorldTextureRect> _bulletRects = new(64);

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
        var map = args.MapUid;
        var query = _entMan.AllEntityQueryEnumerator<BulletHoleComponent, TransformComponent>();
        var expandedBounds = bounds.Enlarged(2f);

        _bulletRects.Clear();
        while (query.MoveNext(out var uid, out var holes, out var xform))
        {
            if (holes.HolePositions.Count == 0 || xform.MapUid != map)
                continue;

            var worldPos = _xform.GetWorldPosition(xform);
            if (!expandedBounds.Contains(worldPos))
                continue;

            // have offsets be done grid-relative, not absolute
            var gridRot = xform.GridUid is { } grid
                ? _xform.GetWorldRotation(grid)
                : Angle.Zero;

            var bulletRot = Matrix3x2.CreateRotation((float) gridRot);
            foreach (var localOffset in holes.HolePositions)
            {
                var worldOffset = Vector2.Transform(localOffset, bulletRot);
                var center = worldPos + worldOffset;

                var quad = new Box2Rotated(HoleBox.Translated(center), gridRot);
                _bulletRects.Add(new(quad));
            }
        }

        if (_bulletRects.Count == 0)
            return;

        handle.SetTransform(Matrix3x2.Identity);
        handle.DrawTextureRectsUnmodulated(texture, CollectionsMarshal.AsSpan(_bulletRects));
    }
}
