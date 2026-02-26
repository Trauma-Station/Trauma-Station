using System.Numerics;
using Content.Shared.Coordinates;
using Content.Trauma.Shared.Weapons.Ranged;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed class BulletholeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan    = default!;
    [Dependency] private readonly IResourceCache _resources = default!;

    private readonly TransformSystem _xform;

    private const string RsiPath  = "/Textures/_RMC14/Effects/bulletholes.rsi";
    private const string RsiState = "bullethole";
    private const float  DrawSize = 1f;

    private Texture? _texture;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public BulletholeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _xform = _entMan.System<TransformSystem>();
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
        var texture = GetTexture();
        if (texture == null)
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var query  = _entMan.AllEntityQueryEnumerator<BulletholeComponent, TransformComponent>();
        var expandedBounds = bounds.Enlarged(2f);

        while (query.MoveNext(out var uid, out var holes, out var xform))
        {
            if (holes.HolePositions.Count == 0)
                continue;

            var worldPos = _xform.GetWorldPosition(uid);

            if (!expandedBounds.Contains(worldPos))
                continue;

            var gridUid = xform.GridUid;
            var gridRot = gridUid != null
                ? _xform.GetWorldRotation(gridUid.Value)
                : Angle.Zero;

            foreach (var (localOffset, _) in holes.HolePositions)
            {
                var worldOffset = Vector2.Transform(localOffset, Matrix3x2.CreateRotation((float)gridRot));
                var center = worldPos + worldOffset;
                var box    = Box2.CenteredAround(Vector2.Zero, new Vector2(DrawSize, DrawSize));

                handle.SetTransform(
                    Matrix3x2.CreateRotation((float)gridRot) *
                    Matrix3x2.CreateTranslation(center));

                handle.DrawTextureRect(texture, box);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
