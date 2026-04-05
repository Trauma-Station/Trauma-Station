// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Server.Plumbing.Components;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed partial class PlumbingLeakSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlumbingDeviceComponent, PlumbingDeviceUpdateEvent>(OnLeak);
    }
    private void OnLeak(Entity<PlumbingDeviceComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var (uid, vent) = ent;

        if (!TryComp<NodeContainerComponent>(uid, out var container) || !TryComp(uid, out TransformComponent? xform))
            return;

        foreach (var node in container.Nodes.Values)
        {

            if (node is not PlumbingNode pNode || pNode.PipeNet is not PlumbingNet net || !pNode.IsLeaking)
                continue;

            var pressure = (net.Liquid.MaxVolume > 0 ? (float) (net.Liquid.Volume / net.Liquid.MaxVolume) : 0f) + net.ExternalPressureForce;

            if (pressure <= 0)
                continue;

            var leakRate = pressure * pNode.Volume * 0.2f * args.FrameTime;
            var spill = net.Liquid.SplitSolution(leakRate);

            if (spill.Volume <= 0)
                continue;

            var openDirs = _plumbing.GetOpenDirections(pNode).ToList();
            if (openDirs.Count == 0) continue;

            var spillPerDir = spill.Volume / openDirs.Count;

            foreach (var dir in openDirs)
            {
                var worldDir = xform.LocalRotation.RotateVec(dir.ToVec());
                var targetCoords = xform.Coordinates.Offset(worldDir);

                var partialSpill = spill.SplitSolution(spillPerDir);
                _puddle.TrySpillAt(targetCoords, partialSpill, out _, false); // would love sound, but it spams the shit so much lmao.
            }
        }
    }
}
