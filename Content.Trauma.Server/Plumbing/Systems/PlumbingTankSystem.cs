// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Shared.Plumbing;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed class PlumbingTankSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FluidTankComponent, PlumbingDeviceUpdateEvent>(OnTankUpdate);
        SubscribeLocalEvent<FluidTankComponent, AfterInteractUsingEvent>(OnAfterInteract);
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

    private void OnAfterInteract(Entity<FluidTankComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // Try to get the solution from the item the player is holding (Beaker/Bucket)
        if (!TryComp<RefillableSolutionComponent>(args.Used, out var refill) || !TryComp<SolutionContainerManagerComponent>(args.Used, out var manager) || !_solution.TryGetRefillableSolution((args.Used, refill, manager), out var itemSol, out var solution))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.BufferName, out var tankSol) || tankSol is not { } buffer)
            return;

        var transferred = _solution.TryTransferSolution(buffer, solution, ent.Comp.TransferAmount);

        if (transferred)
            args.Handled = true;
    }
}
