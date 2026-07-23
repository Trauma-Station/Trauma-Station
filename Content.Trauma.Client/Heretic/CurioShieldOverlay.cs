// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;
using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Heretic;

public sealed partial class CurioShieldOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private static readonly ProtoId<ShaderPrototype> Shader = "GridPulse";

    private readonly List<WorldTextureRect> _rects = new();
    private readonly ShaderInstance _shader;

    public CurioShieldOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = (int) Content.Shared.DrawDepth.DrawDepth.FloorEffects;

        _transform = _entMan.System<SharedTransformSystem>();

        _shader = _proto.Index(Shader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(3f);
        var curTime = _timing.CurTime;

        _rects.Clear();

        var query = _entMan.EntityQueryEnumerator<UnfathomableCurioShieldComponent, TransformComponent>();
        while (query.MoveNext(out _, out var shield, out var xform))
        {
            var factor = shield.Active
                ? InverseLerp(shield.ActivateTime,
                    shield.ActivateTime + shield.FadeTime,
                    curTime)
                : 1f - InverseLerp(shield.DeactivateTime, shield.DeactivateTime + shield.FadeTime, curTime);

            if (factor <= 0f)
                continue;

            var pos = _transform.GetWorldPosition(xform);

            if (!bounds.Contains(pos))
                continue;

            var box = Box2.CenteredAround(pos, new Vector2(shield.SlowdownRadius * factor * 4f));
            _rects.Add(new(new(box), shield.Color));
        }

        if (_rects.Count == 0)
            return;

        handle.UseShader(_shader);
        // We draw textures instead of shapes so that shader can actually use UV parameter
        handle.DrawTextureRectsUnmodulated(Texture.White, CollectionsMarshal.AsSpan(_rects));
        handle.UseShader(null);
    }

    private float InverseLerp(TimeSpan min, TimeSpan max, TimeSpan value)
    {
        return max <= min ? 1f : (float) Math.Clamp((value - min) / (max - min), 0f, 1f);
    }
}
