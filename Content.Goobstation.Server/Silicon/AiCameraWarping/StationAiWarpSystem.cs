// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Silicon.AiCameraWarping;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Systems;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.Silicon.AiCameraWarping;

public sealed partial class StationAiWarpSystem : SharedStationAiWarpSystem
{
    [Dependency] private SharedStationAiSystem _ai = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiHeldComponent, CameraWarpActionMessage>(OnUiWarpAction);
        SubscribeLocalEvent<StationAiHeldComponent, CameraWarpRefreshActionMessage>(OnUiRefreshRequested);
        SubscribeLocalEvent<StationAiHeldComponent, ToggleCameraWarpScreenEvent>(ToggleCameraWarpScreen);
    }

    private void OnUiRefreshRequested(Entity<StationAiHeldComponent> ent, ref CameraWarpRefreshActionMessage args)
    {
        if (!_ai.TryGetCore(ent.Owner, out var core))
            return;

        var cameras = GetCameras(core.Owner);

        var state = new CameraWarpBuiState(cameras);
        _ui.SetUiState(ent.Owner, CamWarpUiKey.Key, state);
    }

    private void ToggleCameraWarpScreen(Entity<StationAiHeldComponent> ent, ref ToggleCameraWarpScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent.Owner, out var actor))
            return;
        if (!_ai.TryGetCore(ent.Owner, out var core))
            return;

        args.Handled = true;

        _ui.TryToggleUi(ent.Owner, CamWarpUiKey.Key, actor.PlayerSession);

        var cameras = GetCameras(core.Owner);

        var state = new CameraWarpBuiState(cameras);
        _ui.SetUiState(ent.Owner, CamWarpUiKey.Key, state);
    }

    private List<CameraWarpData> GetCameras(EntityUid coreUid)
    {
        List<CameraWarpData> cameras = new();

        var query = EntityQueryEnumerator<SurveillanceCameraComponent>();

        var aiGrid = _transform.GetGrid(coreUid);

        while (query.MoveNext(out var queryUid, out var comp))
        {
            if (_transform.GetGrid(queryUid) != aiGrid || !comp.Active)
                continue;

            var data = new CameraWarpData
            {
                NetEntityUid = GetNetEntity(queryUid),
                DisplayName = comp.CameraId
            };

            cameras.Add(data);
        }

        return cameras;
    }

    private void OnUiWarpAction(Entity<StationAiHeldComponent> ent, ref CameraWarpActionMessage args)
    {
        if (args.WarpAction == null)
            return;

        var target = GetEntity(args.WarpAction.Target);

        // The UI doesn't get refreshed when a camera goes offline or whatever
        // so we gotta check if it can still be jumped to.

        if (!TryComp<SurveillanceCameraComponent>(target, out var camera) || !camera.Active)
            return;

        if (!_ai.TryGetCore(ent.Owner, out var core) || core.Comp?.RemoteEntity == null)
            return;

        // The AI shouldn't be able to jump to cams on other stations/shuttles
        if (_transform.GetGrid(core.Owner) != _transform.GetGrid(target))
            return;

        _transform.SetWorldPosition(core.Comp.RemoteEntity.Value, _transform.GetWorldPosition(target));
    }
}
