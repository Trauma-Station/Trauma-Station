// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost.Roles.Components;
using Content.Server.Silicons.Borgs;
using Content.Server.Tools.Innate;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Robotics;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Trauma.Shared.Drone;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Drone;

public sealed partial class ServerDroneSystem : DroneSystem
{
    [Dependency] private BorgSystem _borg = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InnateToolSystem _innateTool = default!;
    [Dependency] private SharedDeviceNetworkSystem _deviceNet = default!;

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<DroneComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TryComp<InnateToolComponent>(ent, out var innate))
            _innateTool.Cleanup(ent, innate);

        _gibbing.Gib(ent.Owner);
        QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnMindRemoved(Entity<DroneComponent> ent, ref MindRemovedMessage args)
    {
        UpdateDroneAppearance(ent, false);
        EnsureComp<GhostTakeoverAvailableComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BorgTransponderComponent, DroneComponent, DeviceNetworkComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out  var comp, out var drone, out var device, out var  meta))
        {
            if (now < comp.NextBroadcast)
                continue;

            var hasBrain = HasComp<ActorComponent>(uid);
            var hpPercent = _borg.CalcHP(uid);
            var data = new CyborgControlData(
                comp.Sprite,
                comp.Name,
                meta.EntityName,
                1f,
                hpPercent,
                0,
                hasBrain,
                false);

            var payload = new RoboticsCyborgDataPayload()
            {
                Data = data,
            };
            _deviceNet.SendPacket((uid, device), null, ref payload);

            comp.NextBroadcast = now + comp.BroadcastDelay;
        }
    }
}
