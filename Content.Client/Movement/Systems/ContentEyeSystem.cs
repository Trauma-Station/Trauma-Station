using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;

namespace Content.Client.Movement.Systems;

public sealed partial class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private IPlayerManager _player = default!;

    public void RequestZoom(EntityUid uid, Vector2 zoom, bool ignoreLimit, bool scalePvs, ContentEyeComponent? content = null)
    {
        if (!Resolve(uid, ref content, false))
            return;

        RaisePredictiveEvent(new RequestTargetZoomEvent()
        {
            TargetZoom = zoom,
            IgnoreLimit = ignoreLimit,
        });

        if (scalePvs)
            RequestPvsScale(Math.Max(zoom.X, zoom.Y));
    }

    public void RequestPvsScale(float scale)
    {
        RaiseNetworkEvent(new RequestPvsScaleEvent(scale));
    }

    public void RequestToggleFov()
    {
        if (_player.LocalEntity is { } player)
            RequestToggleFov(player);
    }

    public void RequestToggleFov(EntityUid uid, EyeComponent? eye = null)
    {
        if (Resolve(uid, ref eye, false))
            RequestEye(!eye.DrawFov, eye.DrawLight);
    }

    public void RequestToggleLight(EntityUid uid, EyeComponent? eye = null)
    {
        if (Resolve(uid, ref eye, false))
            RequestEye(eye.DrawFov, !eye.DrawLight);
    }


    public void RequestEye(bool drawFov, bool drawLight)
    {
        RaisePredictiveEvent(new RequestEyeEvent(drawFov, drawLight));
    }

    // <Trauma> - make them call a shared method instead of copy pasting everything, which only processes the LocalEntity not everything in pvs!!
    private void UpdateLocalEye()
    {
        if (_player.LocalEntity is { } uid && TryComp<EyeComponent>(uid, out var eye))
            UpdateEyeOffset((uid, eye));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        UpdateLocalEye();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateLocalEye();
    }
    // </Trauma>
}
