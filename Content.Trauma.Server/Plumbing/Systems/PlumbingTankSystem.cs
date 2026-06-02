// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Shared.Plumbing;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed partial class PlumbingTankSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private PlumbingSystem _plumbing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FluidTankComponent, PlumbingDeviceUpdateEvent>(OnTankUpdate);
    }

    private void OnTankUpdate(Entity<FluidTankComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        if (!TryComp<NodeContainerComponent>(ent, out var container) ||
            !container.Nodes.TryGetValue(ent.Comp.NodeName, out var node) ||
            node is not PlumbingNode pNode || pNode.PipeNet is not PlumbingNet net)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferName, out var bufferNullable) || bufferNullable is not { } buffer)
            return;

        var netPressure = (float) (net.Liquid.Volume / net.Liquid.MaxVolume) + net.ExternalPressureForce;
        var tankPressure = (float) (buffer.Comp.Solution.Volume / buffer.Comp.Solution.MaxVolume);

        var diff = netPressure - tankPressure;

        if (diff > 0.01f) // Net pushes into tank
        {
            var move = net.Liquid.SplitSolution(net.Liquid.Volume * 0.1f * args.FrameTime);
            _solution.TryAddSolution(buffer, move);
        }
        else if (diff < -0.01f) // Tank drains into net
        {
            var move = _solution.SplitSolution(buffer, buffer.Comp.Solution.Volume * 0.1f * args.FrameTime);
            _plumbing.InjectIntoNet(net, move);
        }
    }
}
