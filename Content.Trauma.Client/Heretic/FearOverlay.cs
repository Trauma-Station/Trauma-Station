// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Examine;
using Content.Shared.Humanoid;
using Content.Trauma.Shared.Heretic.Components.Side;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Random;

namespace Content.Trauma.Client.Heretic;

public sealed class FearOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "Horror";

    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly ExamineSystem _examine;

    private readonly HashSet<Entity<SpriteComponent, TransformComponent>> _hiddenEntities = new();
    private readonly HashSet<Entity<SpriteComponent>> _visibleFearTargets = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities | OverlaySpace.WorldSpaceEntities |
                                          OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public FearOverlay()
    {
        IoCManager.InjectDependencies(this);

        _shader = _proto.Index(Shader).InstanceUnique();

        _sprite = _entMan.System<SpriteSystem>();
        _examine = _entMan.System<ExamineSystem>();
        _transform = _entMan.System<TransformSystem>();

        ZIndex = (int) Content.Shared.DrawDepth.DrawDepth.Mobs;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye == _eye.CurrentEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye)
            return;

        if (_player.LocalEntity is not { } player || !_entMan.TryGetComponent(player, out FearComponent? fear))
            return;

        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        switch (args.Space)
        {
            case OverlaySpace.WorldSpace:
                UnhideEntities();
                DrawOverlay((player, fear), handle, viewport);
                break;
            case OverlaySpace.WorldSpaceBelowEntities:
                HideEntities((player, fear));
                break;
            case OverlaySpace.WorldSpaceEntities:
                ReplaceSprites(handle, eye.Rotation);
                break;
        }
    }

    private void ReplaceSprites(DrawingHandleWorld handle, Angle eyeRot)
    {
        foreach (var (uid, sprite, xform) in _hiddenEntities)
        {
            var random = new Random(_entMan.GetNetEntity(uid).Id);
            var toRender = random.Pick(_visibleFearTargets);
            var (pos, rot) = _transform.GetWorldPositionRotation(xform);
            _sprite.RenderSprite(toRender,
                handle,
                eyeRot + toRender.Comp.Rotation - sprite.Rotation,
                rot - toRender.Comp.Rotation + sprite.Rotation,
                pos - (-eyeRot).RotateVec(toRender.Comp.Offset) + (-eyeRot).RotateVec(sprite.Offset));
        }
    }

    private void HideEntities(Entity<FearComponent> ent)
    {
        if (ent.Comp.TotalFear < ent.Comp.HorrorThreshold)
            return;

        _visibleFearTargets.Clear();

        foreach (var uid in ent.Comp.FearData.Keys)
        {
            if (uid == ent.Owner || !_entMan.EntityExists(uid) ||
                !_entMan.TryGetComponent(uid, out SpriteComponent? sprite) || !sprite.Visible ||
                !_examine.InRangeUnOccluded(ent.Owner, uid))
                continue;

            _visibleFearTargets.Add((uid, sprite));
        }

        if (_visibleFearTargets.Count == 0)
            return;

        var query = _entMan.EntityQueryEnumerator<HumanoidProfileComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (!sprite.Visible || uid == ent.Owner)
                continue;

            _sprite.SetVisible((uid, sprite), false);
            _hiddenEntities.Add((uid, sprite, xform));
        }
    }

    private void UnhideEntities()
    {
        foreach (var ent in _hiddenEntities)
        {
            _sprite.SetVisible(ent.AsNullable(), true);
        }

        _hiddenEntities.Clear();
    }

    private void DrawOverlay(Entity<FearComponent> ent, DrawingHandleWorld handle, Box2Rotated viewport)
    {
        var zoom = _entMan.TryGetComponent(ent, out EyeComponent? eye) ? eye.Zoom.X : 1f;
        var radius = MathF.Max(ent.Comp.MinRadius, 1f - ent.Comp.TotalFear / ent.Comp.MaxFear);
        _shader.SetParameter("radius", radius);
        _shader.SetParameter("zoom", zoom);

        handle.UseShader(_shader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
