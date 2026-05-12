// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eye;
using Content.Shared.MouseRotator;
using Content.Trauma.Client.Viewcone.ComponentTree;
using Content.Trauma.Shared.Viewcone;
using Content.Trauma.Shared.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

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
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly ViewconeOverlaySystem _cone;
    private readonly ViewconeAngleSystem _angle;
    private readonly ViewconeOcclusionSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<ViewconeClientOverrideComponent> _overrideQuery;
    private readonly EntityQuery<ViewconeOccludedComponent> _occludedQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    // slightly sus but cached from beforedraw to use in draw.
    private Entity<EyeComponent, ViewconeComponent>? _nextEye;

    public ViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.System<ViewconeOverlaySystem>();
        _angle = _ent.System<ViewconeAngleSystem>();
        _tree = _ent.System<ViewconeOcclusionSystem>();
        _xform  = _ent.System<TransformSystem>();
        _sprite = _ent.System<SpriteSystem>();

        _spriteQuery = _ent.GetEntityQuery<SpriteComponent>();
        _overrideQuery = _ent.GetEntityQuery<ViewconeClientOverrideComponent>();
        _occludedQuery = _ent.GetEntityQuery<ViewconeOccludedComponent>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _nextEye = null;

        // TODO: rt pr to add Entity<EyeComponent>? Entity to IEye then just use ?.Entity here
        if (args.Viewport.Eye == null)
            return false;

        // This is really stupid but there isn't another way to reverse an eye entity from just an IEye afaict
        // It's not really inefficient though. theres only at most a few of these inside PVS anyway
        var enumerator = _ent.AllEntityQueryEnumerator<LerpingEyeComponent, EyeComponent, ViewconeComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone))
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
        var eyePos = _xform.GetWorldPosition(eyeTransform);
        var eyeRot = cone.ViewAngle - eye.Rotation; // subtract rotation cuz idk. the lerp adds it but this doesnt want it for some reason idk.

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !! Thank You Bhijn God (TYBG) for 95% of the rest of this methods code !!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        var radConeAngle = MathHelper.DegreesToRadians(_angle.GetAngle((ent, cone)));
        var halfAngle = radConeAngle * 0.5f;
        var radConeFeather = MathHelper.DegreesToRadians(cone.ConeFeather);

        _cone.CachedBaseAlphas.Clear();
        var occludables = _tree.QueryAabb(args.MapId, args.WorldBounds);
        var fadeTime = cone.FadeTime.TotalSeconds;
        var now = _timing.CurTime;
        foreach (var entry in occludables)
        {
            var (comp, xform) = entry;
            var uid = entry.Uid;

            // dynamic clientside disabling, for effects like pulled entities
            if (_overrideQuery.HasComp(uid))
                continue;

            if (!_spriteQuery.TryComp(uid, out var sprite))
                continue;

            if (comp.Source == ent || uid == ent)
                continue; // sentient walls should be allowed to see things

            if (!comp.OccludeIfAnchored && xform.Anchored)
                continue;

            var (entPos, entRot) = _xform.GetWorldPositionRotation(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Math.Abs(Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot).Theta);

            // handle fading logic, things fade out over time when you dont look at them
            // when they are out of view you can't see where they are right now
            ViewconeOccludedComponent? occluded;
            var targetAlpha = 1f;
            if (angleDist > halfAngle)
            {
                // outside vision, lock old position from the "memory"
                // won't work with animations and stuff, tough
                if (!_ent.EnsureComponent(uid, out occluded))
                {
                    // occluded for the first frame, copy original sprite data
                    occluded.LastSeen = now;
                    occluded.LastPosition = entPos + sprite.Offset;
                    occluded.OriginalOffset = sprite.Offset;
                    occluded.LastRotation = entRot + sprite.Rotation;
                    occluded.OriginalRotation = sprite.Rotation;
                }

                // offset it so moving mobs etc stay where they were last seen
                _sprite.SetOffset((uid, sprite), occluded.LastPosition - entPos);
                _sprite.SetRotation((uid, sprite), occluded.LastRotation - entRot);

                // the actual fading
                var diff = now - occluded.LastSeen;
                if (diff >= cone.FadeStart)
                    targetAlpha -= (float) Math.Min(1.0, (diff - cone.FadeStart).TotalSeconds / fadeTime);
            }
            else if (_occludedQuery.TryComp(uid, out occluded))
            {
                // in vision now, revert to old values
                _sprite.SetOffset((uid, sprite), occluded.OriginalOffset);
                _sprite.SetRotation((uid, sprite), occluded.OriginalRotation);
                _ent.RemoveComponent(uid, occluded);
            }

            var baseAlpha = sprite.Color.A;

            // save the results so we can use it in resetalpha overlay
            _cone.CachedBaseAlphas.Add(((uid, sprite), baseAlpha));

            // multiply by the base alpha of the sprite (sprites which were already invisible for other reasons should stay invisible)
            var alpha = (comp.Inverted ? 1f - targetAlpha : targetAlpha) * (comp.OverrideBaseAlpha ? 1f : baseAlpha);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
            _sprite.SetVisible((uid, sprite), alpha > 0f);
        }
    }
}
