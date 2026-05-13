// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eye;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Pulling.Events;
using Content.Trauma.Client.Viewcone.Overlays;
using Content.Trauma.Common.Popups;
using Content.Trauma.Shared.Viewcone;
using Content.Trauma.Shared.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Viewcone;

/// <summary>
/// Handles adding and removing the viewcone overlays, as well as ferrying data between them
/// Also handles calculating desired view angle for active viewcones so overlays can use it
/// </summary>
public sealed class ViewconeOverlaySystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ViewconeAngleSystem _angle = default!;
    [Dependency] private readonly EntityQuery<MouseRotatorComponent> _rotatorQuery = default!;

    private ViewconeConeOverlay _coneOverlay = default!;
    private ViewconeSetAlphaOverlay _setAlphaOverlay = default!;
    private ViewconeResetAlphaOverlay _resetAlphaOverlay = default!;

    private const float LerpHalfLife = 0.065f;

    // slightly balls state management, but
    // done so we don't have to requery within the same frame
    // this is always cleared at the end of resetting alpha
    // it is the least thread safe code of all time obviously. but rendering not threaded. so
    // we can abuse the fact that the overlays will always draw sequentially in the order we expect, and
    // one wont start rendering in the middle of rendering another
    internal List<(Entity<SpriteComponent> ent, float baseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ViewconeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ViewconeComponent, ShowPopupAttemptEvent>(OnShowPopupAttempt);

        SubscribeLocalEvent<ViewconeComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ViewconeComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ViewconeOccludableComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<ViewconeOccludableComponent, PullStoppedMessage>(OnPullStopped);
        SubscribeLocalEvent<ViewconeOccludableComponent, ComponentShutdown>(OnOccludableShutdown);

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // the reason we use lerpingeye here in the query first is to
        // specifically check for eyes that we are actually rendering (lerpingeye already handles this sort of
        // its like jank as fuck in that system but whatever thats like not my problem )
        var enumerator = AllEntityQuery<LerpingEyeComponent, EyeComponent, ViewconeComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone, out var xform))
        {
            // cached for overlays and popups to use
            viewcone.CurrentConeAngle = _angle.GetAngle((uid, viewcone));

            var eyeAngle = eye.Rotation;
            var (position, rotation) = _xform.GetWorldPositionRotation(xform);
            var playerAngle = rotation;
            var desiredWasNull = viewcone.DesiredViewAngle == null;

            if (_rotatorQuery.HasComp(uid))
            {
                var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
                if (mousePos.MapId != MapId.Nullspace)
                    playerAngle = (mousePos.Position - _xform.GetMapCoordinates(xform).Position).ToAngle() + Angle.FromDegrees(90);

                viewcone.LastMouseRotationAngle = playerAngle;
            }
            else if (viewcone.LastMouseRotationAngle != 0f)
            {
                // if last frame we had a mouse rotation angle, but now we dont,
                // that means it was disabled
                // but, we should keep the old mouse angle for viewcone, at least until the real angle actually changes
                // or they move
                if (MathHelper.CloseToPercent(viewcone.LastWorldRotationAngle, playerAngle, .001d)
                    && viewcone.LastWorldPos == position)
                {
                    playerAngle = viewcone.LastMouseRotationAngle;
                }
                else
                {
                    viewcone.LastMouseRotationAngle = 0f;
                }
            }

            viewcone.LastWorldPos = position;
            viewcone.LastWorldRotationAngle = rotation;
            viewcone.DesiredViewAngle = playerAngle + eyeAngle;

            // if desired angle was null before we set it
            // then just set viewangle to it immediately
            // (assume it was first frame)
            if (desiredWasNull)
            {
                viewcone.ViewAngle = viewcone.DesiredViewAngle.Value;
                continue;
            }

            // framerate-independent lerp
            // https://twitter.com/FreyaHolmer/status/1757836988495847568
            // convert to angle first so we lerp thru shortestdistance
            viewcone.ViewAngle = Angle.Lerp(viewcone.ViewAngle, viewcone.DesiredViewAngle.Value, 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife)));
        }
    }

    /// <summary>
    /// Returns true if a point is inside the vision cone, using world positions.
    /// </summary>
    public bool IsVisible(Entity<ViewconeComponent> ent, Vector2 eyePos, Vector2 pos)
    {
        var dist = pos - eyePos;
        var r = ent.Comp.ConeIgnoreRadius;
        var r2 = r * r;
        if (dist.LengthSquared() < r2)
            return true; // within cone ignore radius so always visible regardless of angle

        var eyeRot = ent.Comp.ViewAngle;
        var angleDist = Math.Abs(Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot).Theta);
        return angleDist < MathHelper.DegreesToRadians(ent.Comp.CurrentConeAngle) * 0.5f;
    }

    private void OnPlayerAttached(Entity<ViewconeComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<ViewconeComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void OnInit(Entity<ViewconeComponent> ent, ref ComponentInit args)
    {
        if (ent.Owner == _player.LocalEntity)
            AddOverlays();
    }

    private void OnShowPopupAttempt(Entity<ViewconeComponent> ent, ref ShowPopupAttemptEvent args)
    {
        args.Cancelled |= !IsVisible(ent, args.ViewerPos, args.WorldPos);
    }

    private void OnShutdown(Entity<ViewconeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlay.AddOverlay(_coneOverlay);
        _overlay.AddOverlay(_setAlphaOverlay);
        _overlay.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlay.RemoveOverlay(_coneOverlay);
        _overlay.RemoveOverlay(_setAlphaOverlay);
        _overlay.RemoveOverlay(_resetAlphaOverlay);
    }

    // Logic for disabling occluding of entities that you're currently pulling.
    private void OnPullStarted(Entity<ViewconeOccludableComponent> ent, ref PullStartedMessage args)
    {
        // can this even happen? idk
        if (args.PullerUid != _player.LocalEntity || !_timing.IsFirstTimePredicted)
            return;

        EnsureComp<ViewconeClientOverrideComponent>(ent);
    }

    private void OnPullStopped(Entity<ViewconeOccludableComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PullerUid != _player.LocalEntity)
            return;

        // why the fuck can this even happen? it stops the pull clientside and never restarts it?
        // is clientside pulling just bugged upstream?
        // the flow is "applying state -> reset virtual hand ent -> delete it (??) -> AUGHHHH THAT MEANS STOP PULLING I GUESS"
        if (_timing.ApplyingState)
            return;

        RemComp<ViewconeClientOverrideComponent>(ent);
    }

    private void OnOccludableShutdown(Entity<ViewconeOccludableComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Memory is { } memory && !TerminatingOrDeleted(memory))
            Del(memory);
    }
}
