// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eye;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Trauma.Client.Viewcone.ComponentTree;
using Content.Trauma.Shared.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
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
public sealed partial class ViewconeSetAlphaOverlay : Overlay
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;
    private readonly MetaDataSystem _meta;
    private readonly SharedContainerSystem _container;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _xform;
    private readonly ViewconeOverlaySystem _cone;
    private readonly ViewconeOcclusionSystem _tree;

    private readonly EntityQuery<HumanoidProfileComponent> _humanoidQuery;
    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<ViewconeClientOverrideComponent> _overrideQuery;
    private readonly EntityQuery<ViewconeOccludedComponent> _occludedQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public static readonly EntProtoId MemoryEntity = "ViewconeMemory";

    // slightly sus but cached from beforedraw to use in draw.
    private Entity<EyeComponent, ViewconeComponent>? _nextEye;

    public ViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _meta = _ent.System<MetaDataSystem>();
        _container = _ent.System<SharedContainerSystem>();
        _sprite = _ent.System<SpriteSystem>();
        _xform  = _ent.System<TransformSystem>();
        _cone = _ent.System<ViewconeOverlaySystem>();
        _tree = _ent.System<ViewconeOcclusionSystem>();

        _humanoidQuery = _ent.GetEntityQuery<HumanoidProfileComponent>();
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
        var radConeAngle = MathHelper.DegreesToRadians(cone.CurrentConeAngle);
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

            if (_container.IsEntityInContainer(uid))
            {
                if (comp.Memory is { } containedMemory)
                    _sprite.SetVisible(containedMemory, false);
                continue; // completely ignore anything in a container since it won't be visible anyway
            }

            var (entPos, entRot) = _xform.GetWorldPositionRotation(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Math.Abs(Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot).Theta);

            // calculate opacity for the actual entity first
            var baseAlpha = sprite.Color.A;
            var angleAlpha = (float) Math.Clamp((angleDist - halfAngle) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp((distLength - cone.ConeIgnoreRadius) + (cone.ConeIgnoreFeather * 0.5f), 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = 1f - Math.Min(angleAlpha, distAlpha);

            // simplified logic for effects that dont spawn memories or anything likely stealthed
            if (!comp.UseMemory || ((!sprite.Visible || sprite.Color.A < 0.4) && !_occludedQuery.HasComp(uid)))
            {
                // don't want to show memory for invisible things
                if (comp.Memory is { } oldMemory)
                    _sprite.SetVisible(oldMemory, false);

                // save the results so we can use it in resetalpha overlay
                _cone.CachedBaseAlphas.Add(((uid, sprite), baseAlpha));

                // multiply by the base alpha of the sprite (sprites which were already invisible for other reasons should stay invisible)
                var alpha = (comp.Inverted ? 1f - targetAlpha : targetAlpha) * (comp.OverrideBaseAlpha ? 1f : baseAlpha);
                SetAlpha((uid, sprite), alpha);
                continue;
            }

            ViewconeOccludedComponent? occluded = null;
            if (targetAlpha > 0.001f)
            {
                // update time for fading whenever you see it
                comp.LastSeen = now;
                if (!_occludedQuery.TryComp(uid, out occluded))
                    continue; // most of the time we stop here, this only happens once until it leaves view again

                // hide the memory if it goes back in view
                if (comp.Memory is { } oldMemory)
                    _sprite.SetVisible(oldMemory, false);
                // and show the real entity again
                _sprite.SetVisible((uid, sprite), true);
                _ent.RemoveComponent(uid, occluded);
                continue;
            }

            // when things go out of view, they get a memory in their place
            comp.Memory ??= _ent.SpawnEntity(MemoryEntity, xform.Coordinates);
            var memory = comp.Memory.Value;
            if (!_ent.EnsureComponent<ViewconeOccludedComponent>(uid, out occluded))
            {
                // occluded for the first frame, copy original sprite data to memory entity
                _xform.SetCoordinates(memory, xform.Coordinates);
                _xform.SetLocalRotation(memory, xform.LocalRotation);
                _meta.SetEntityName(memory, Identity.Name(uid, _ent));
                _sprite.CopySprite((uid, sprite), memory);
                // don't show the real entity
                _sprite.SetVisible((uid, sprite), false);
            }

            if (!_spriteQuery.TryComp(memory, out var memorySprite))
            {
                // try again next frame? should never happen
                _ent.DeleteEntity(memory);
                comp.Memory = null;
                continue;
            }

            var memoryVisible = _cone.IsVisible((ent, cone), eyePos, _xform.GetWorldPosition(memory));

            var diff = now - comp.LastSeen;
            // FIXME: this looks awful for people because the sprite opacity is applied to each layer instead of being deferred somehow
            if (_humanoidQuery.HasComp(uid))
            {
                _sprite.SetVisible((memory, memorySprite), (diff.TotalSeconds < fadeTime) && !memoryVisible);
                continue;
            }

            // TOS reference..?
            var memoryAlpha = diff < cone.FadeStart
                ? 1f
                : 1f - (float) Math.Min(1.0, (diff - cone.FadeStart).TotalSeconds / fadeTime);
            if (memoryVisible)
                memoryAlpha = 0f; // if you can see where a memory was and it's not there, the memory must be wrong
            // now actually fade the memory out
            SetAlpha((memory, memorySprite), memoryAlpha);
        }
    }

    private void SetAlpha(Entity<SpriteComponent> ent, float alpha)
    {
        var e = ent.AsNullable();
        _sprite.SetColor(e, ent.Comp.Color.WithAlpha(alpha));
        _sprite.SetVisible(e, alpha > 0.01f);
    }
}
