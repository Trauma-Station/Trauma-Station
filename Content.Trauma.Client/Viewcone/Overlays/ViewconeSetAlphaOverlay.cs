// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.MouseRotator;
using Content.Trauma.Client.Viewcone.ComponentTree;
using Content.Trauma.Shared.Viewcone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Trauma.Client.Viewcone.Overlays;

/// <summary>
/// Queries the bounds for each viewport for all <see cref="ViewconeOccludableComponent"/>, then
/// sets their alpha before entities render in accordance with whether they should be in view or not
///
/// This alpha pass only works because of <see cref="ViewconeResetAlphaOverlay"/>, which resets in a later stage of rendering.
/// </summary>
public sealed class ViewconeSetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    private readonly ViewconeOverlaySystem _cone;
    private readonly ViewconeOcclusionSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    // slightly sus but cached from beforedraw to use in draw.
    private Entity<EyeComponent, ViewconeComponent>? _nextEye;

    public ViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.System<ViewconeOverlaySystem>();
        _tree = _ent.System<ViewconeOcclusionSystem>();
        _xform  = _ent.System<TransformSystem>();
        _sprite = _ent.System<SpriteSystem>();

        _spriteQuery = _ent.GetEntityQuery<SpriteComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _nextEye = null;

        // TODO: rt pr to add Entity<EyeComponent>? Entity to IEye then just use ?.Entity here
        if (args.Viewport.Eye == null)
            return false;

        // This is really stupid but there isn't another way to reverse an eye entity from just an IEye afaict
        // It's not really inefficient though. theres only at most a few of these inside PVS anyway
        var enumerator = _ent.AllEntityQueryEnumerator<ViewconeComponent, EyeComponent>();
        while (enumerator.MoveNext(out var uid, out var viewcone, out var eye))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _nextEye = (uid, eye, viewcone);
            break;
        }

        return _nextEye != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_nextEye == null)
            return;

        var (ent, eye, cone) = _nextEye.Value;

        var eyeTransform = _ent.GetComponent<TransformComponent>(ent);
        var (eyePos, eyeRot) = _xform.GetWorldPositionRotation(eyeTransform);

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !! Thank You Bhijn God (TYBG) for 95% of the rest of this methods code !!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        if (_ent.HasComponent<MouseRotatorComponent>(ent))
        {
            // this should work for multiviewport. at least, about as well as people will expect
            // this wont run for other eye entities that have viewcones
            // (if any even end up existing.. I probably made all this code viewport agnostic for no reason)
            // (but it'd be nice to have cameras that have viewcones. right. right)
            // (Withers )
            // but for a separate viewport following the same mouserotator entity, idk, it probably works fine.
            // when is that even going to happen.
            var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
            if (mousePos.MapId == eyeTransform.MapID)
                eyeRot = (mousePos.Position - _xform.GetMapCoordinates(eyeTransform).Position).ToWorldAngle();
        }

        var radConeAngle = MathHelper.DegreesToRadians(cone.ConeAngle);
        var radConeFeather = MathHelper.DegreesToRadians(cone.ConeFeather);

        _cone.CachedBaseAlphas.Clear();
        var occludables = _tree.QueryAabb(args.MapId, args.WorldBounds);
        foreach (var entry in occludables)
        {
            var (comp, xform) = entry;
            var uid = entry.Uid; // this uses component.Owner internally... oh well

            if (!_spriteQuery.TryComp(uid, out var sprite))
                continue;

            if (comp.Source == ent)
                continue; // sentient walls should be allowed to see things

            if (!comp.OccludeIfAnchored && xform.Anchored)
                continue;

            var entPos = _xform.GetWorldPosition(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

            var angleAlpha = (float) Math.Clamp((Math.Abs(angleDist.Theta) - (radConeAngle * 0.5f)) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp((distLength - cone.ConeIgnoreRadius) + (cone.ConeIgnoreFeather * 0.5f), 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

            // save the results so we can use it in resetalpha overlay
            _cone.CachedBaseAlphas.Add(((uid, sprite), sprite.Color.A));

            var alpha = comp.Inverted ? 1f - targetAlpha : targetAlpha;
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
        }
    }
}
