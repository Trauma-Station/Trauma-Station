// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Projectiles;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Projectiles;

public sealed partial class DodgeEffectOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderProto = "Dodge";

    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    private SharedTransformSystem _transform = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;
    private const float EffectWorldSize = 1.75f;

    public DodgeEffectOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(ShaderProto).InstanceUnique();
        _transform = _entMan.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var now = _timing.RealTime;

        var query = _entMan.EntityQueryEnumerator<DodgeEffectComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dodge, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var worldPos = _transform.GetWorldPosition(uid);
            var elapsed = (float) (now - dodge.Time).TotalSeconds;
            var progress = Math.Clamp(elapsed / dodge.Duration, 0f, 1f);

            var screenCenter = args.Viewport.WorldToLocal(worldPos);
            screenCenter.Y = args.Viewport.Size.Y - screenCenter.Y;

            var pixelsPerMeter = EyeManager.PixelsPerMeter * args.Viewport.RenderScale;
            var screenSize = new Vector2(EffectWorldSize, EffectWorldSize) * pixelsPerMeter;

            _shader.SetParameter("progress", progress);
            _shader.SetParameter("center", screenCenter);
            _shader.SetParameter("size", screenSize);
            _shader.SetParameter("seed", dodge.Seed);

            var worldBox = Box2.CenteredAround(worldPos, new Vector2(EffectWorldSize, EffectWorldSize));

            worldHandle.UseShader(_shader);
            worldHandle.DrawRect(worldBox, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
