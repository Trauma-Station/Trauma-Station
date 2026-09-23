// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.SurveillanceCamera;
using Content.Server.SurveillanceCamera;
using Robust.Server.GameStates;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using System.Runtime.InteropServices;

namespace Content.Trauma.Server.SurveillanceCamera;

public sealed partial class MobileSurveillanceCameraSystem : EntitySystem
{
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SurveillanceCameraMonitorSystem _monitor = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var refresh = new ValueList<Entity<SurveillanceCameraMonitorComponent>>();

        var query = EntityQueryEnumerator<HasMobileCamerasSurveillanceCameraMonitorComponent, SurveillanceCameraMonitorComponent>();
        while (query.MoveNext(out var uid, out var active, out var monitor))
        {
            if (monitor.KnownMobileCameras.Count == 0)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            // Collect expired cameras and cache their entity references
            var expiredCameras = new ValueList<(string, EntityUid)>();

            foreach (var (addr, cameraData) in monitor.KnownMobileCameras)
            {
                ref var lastSent = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    monitor.KnownMobileCamerasLastHeartbeatSent, addr, out bool sentExists);
                ref var lastHeartbeat = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    monitor.KnownMobileCamerasLastHeartbeat, addr, out bool hbExists);

                if (!sentExists) lastSent = 0f;
                if (!hbExists) lastHeartbeat = 0f;

                lastSent += frameTime;
                lastHeartbeat += frameTime;

                _monitor.SendHeartbeat(uid, addr, monitor);

                if (lastHeartbeat > SurveillanceCameraMonitorSystem.MaxHeartbeatTime)
                    expiredCameras.Add((addr, GetEntity(cameraData.Item2)));
            }

            // Remove PVS overrides for all viewers in a single pass
            foreach (var player in monitor.Viewers)
            {
                if (!TryComp<ActorComponent>(player, out var actor))
                    continue;

                foreach (var (_, entity) in expiredCameras)
                    _pvsOverride.RemoveSessionOverride(entity, actor.PlayerSession);
            }

            // Remove expired cameras from all dictionaries
            foreach (var (key, _) in expiredCameras)
            {
                monitor.KnownMobileCameras.Remove(key);
                monitor.KnownMobileCamerasLastHeartbeat.Remove(key);
                monitor.KnownMobileCamerasLastHeartbeatSent.Remove(key);
            }

            // Cleanup component if empty
            if (monitor.KnownMobileCameras.Count == 0)
                RemCompDeferred(uid, active);

            // Refresh subnets as clearly something went wrong with the networking
            if (expiredCameras.Count > 0)
                refresh.Add((uid, monitor));
        }

        foreach (var ent in refresh)
        {
            _monitor.RefreshCameras(ent, ent.Comp);
        }
    }
}
