using Content.Trauma.Shared.Spy;
using Robust.Shared.Enums;

namespace Content.Trauma.Client.Spy;

public sealed class ScannerOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;

    private TransformSystem _xform;
    private SpriteSystem _sprite;

    private readonly List<Vector2> _vertices = new(3);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public ScannerOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = 1; // Draw after MultiShaderOverlay so we can reuse shader

        _xform = _entMan.System<TransformSystem>();
        _sprite = _entMan.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        var eyeRot = eye.Rotation;
        var handle = args.WorldHandle;

        var beingScannedQuery = _entMan.GetEntityQuery<BeingScannedComponent>();
        var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
        var spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();

        var query = _entMan.EntityQueryEnumerator<ActiveScannerComponent, TransformComponent>();
        while (query.MoveNext(out var scanner, out var xform))
        {
            var scanned = scanner.ScannedObject;
            if (!_entMan.EntityExists(scanned) || !beingScannedQuery.TryComp(scanned, out var comp) ||
                comp.Shader is not { } shader || !spriteQuery.TryComp(scanned, out var sprite))
                continue;

            var ourPos = _xform.GetWorldPosition(xform, xformQuery);
            var (pos, rot) = _xform.GetWorldPositionRotation(scanned);

            _vertices.Clear();
            _vertices.Add(ourPos);
            var spriteBB = _sprite.CalculateBounds((scanned, sprite), pos, rot, eyeRot);
            _vertices.Add(spriteBB.BottomLeft);
            _vertices.Add(spriteBB.BottomRight);

            handle.UseShader(shader);
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _vertices, Color.White);
            handle.UseShader(null);
        }
    }
}
