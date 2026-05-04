using System.Numerics;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Prototypes;

namespace Content.Client.Decals.Overlays;

// Trauma - completely rewrote decals to be entity based
public sealed class DecalOverlay : GridOverlay
{
    private readonly SpriteSystem _sprites;
    private readonly IEntityManager _entMan;
    private readonly IPrototypeManager _prototypeManager;

    private readonly Dictionary<string, (Texture Texture, bool SnapCardinals)> _cachedTextures = new(64);

    private readonly List<Decal> _decals = new();

    public DecalOverlay(
        SpriteSystem sprites,
        IEntityManager entManager,
        IPrototypeManager prototypeManager)
    {
        _sprites = sprites;
        _entMan = entManager;
        _prototypeManager = prototypeManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.MapId == MapId.Nullspace)
            return;

        var owner = Grid.Owner;

        if (!_entMan.TryGetComponent(owner, out DecalGridComponent? decalGrid) ||
            !_entMan.TryGetComponent(owner, out TransformComponent? xform))
        {
            return;
        }

        if (xform.MapID != args.MapId)
            return;

        // Shouldn't need to clear cached textures unless the prototypes get reloaded.
        var handle = args.WorldHandle;
        var xformSystem = _entMan.System<TransformSystem>();
        var eyeAngle = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        var gridAABB = xformSystem.GetInvWorldMatrix(xform).TransformBox(args.WorldBounds.Enlarged(1f));
        _decals.Clear();
        var query = _entMan.AllEntityQueryEnumerator<DecalComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var decalXform))
        {
            if (comp.Data != default && gridAABB.Contains(decalXform.Coordinates.Position))
                _decals.Add(comp.Data);
        }

        if (_decals.Count == 0)
            return;

        _decals.Sort((x, y) =>
        {
            var zComp = x.ZIndex.CompareTo(y.ZIndex);

            if (zComp != 0)
                return zComp;

            return x.Id.CompareTo(y.Id);
        });

        var (_, worldRot, worldMatrix) = xformSystem.GetWorldPositionRotationMatrix(xform);
        handle.SetTransform(worldMatrix);

        foreach (var decal in _decals)
        {
            if (!_cachedTextures.TryGetValue(decal.Id, out var cache))
            {
                // Nothing to cache, someone messed up
                if (!_prototypeManager.Resolve<DecalPrototype>(decal.Id, out var decalProto))
                    continue;

                cache = (_sprites.Frame0(decalProto.Sprite), decalProto.SnapCardinals);
                _cachedTextures[decal.Id] = cache;
            }

            var cardinal = Angle.Zero;

            if (cache.SnapCardinals)
            {
                var worldAngle = eyeAngle + worldRot;
                cardinal = worldAngle.GetCardinalDir().ToAngle();
            }

            var angle = decal.Angle - cardinal;

            if (angle.Equals(Angle.Zero))
                handle.DrawTexture(cache.Texture, decal.Coordinates, decal.Color);
            else
                handle.DrawTexture(cache.Texture, decal.Coordinates, angle, decal.Color);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
