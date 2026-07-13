// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Heretic;

public sealed partial class CurioShieldOverlay : Overlay
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private static readonly ProtoId<ShaderPrototype> Shader = "GridPulse";

    public CurioShieldOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = (int) Content.Shared.DrawDepth.DrawDepth.FloorEffects;

        _transform = _entMan.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(3f);
        var curTime = _timing.CurTime;

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

            var shader = _prototype.Index(Shader).InstanceUnique();
            shader.SetParameter("color", shield.Color);
            shader.SetParameter("radius", factor * 0.5f);
            handle.UseShader(shader);
            // We draw texture instead of shape so that shader can actually use UV parameter
            handle.DrawTextureRect(Texture.White, Box2.CenteredAround(pos, new Vector2(shield.SlowdownRadius * 4f)));
        }

        handle.UseShader(null);
    }

    private float InverseLerp(TimeSpan min, TimeSpan max, TimeSpan value)
    {
        return max <= min ? 1f : (float) Math.Clamp((value - min) / (max - min), 0f, 1f);
    }
}
