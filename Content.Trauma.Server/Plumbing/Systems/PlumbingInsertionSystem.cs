// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.NodeContainer;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Server.Plumbing.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Trauma.Server.Plumbing.Systems;

public sealed partial class PlumbingInsertionSystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PlumbingSystem _plumbing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FluidPassiveVentComponent, PlumbingDeviceUpdateEvent>(OnPassiveUpdate);
    }

    private void OnPassiveUpdate(Entity<FluidPassiveVentComponent> ent, ref PlumbingDeviceUpdateEvent args)
    {
        var (uid, vent) = ent;

        if (!TryComp<NodeContainerComponent>(uid, out var container))
            return;

        if (!container.Nodes.TryGetValue(ent.Comp.InletName, out var node) || node is not PlumbingNode pNode || pNode.PipeNet is not PlumbingNet net)
            return;

        if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid is not { } || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var tile = _map.GetTileRef((xform.GridUid.Value, grid), xform.Coordinates);
        var availableSpace = net.Liquid.MaxVolume - net.Liquid.Volume;
        var pipePressure = (float) (net.Liquid.Volume / net.Liquid.MaxVolume) + net.ExternalPressureForce;

        if (!_puddle.TryGetPuddle(tile, out var puddleUid) || !TryComp<PuddleComponent>(puddleUid, out var puddleComp) || puddleComp.Solution is not { } puddleSol)
        {
            if (pipePressure > 1.0f)
                Backflow(net, ent, -availableSpace);
            return;
        }

        var puddlePressure = (float) (puddleSol.Comp.Solution.Volume / 50f); // lowkey 50 is a magic number, should replace it will like fluid density or some shit.
        var pressureDiff = puddlePressure - pipePressure;
        if (pressureDiff > 0)
        {
            if (availableSpace > 0)
                Drain(net, puddleUid, puddleSol, pressureDiff, availableSpace, vent.TransferRate, args.FrameTime);
            else
                net.ExternalPressureForce += pressureDiff;
        }
        else if (pipePressure > 1.0f && puddlePressure < pipePressure)
            Backflow(net, ent, -availableSpace);
    }

    private void Drain(PlumbingNet net, EntityUid puddleUid, Entity<SolutionComponent> puddleSol, float pressureDiff, FixedPoint2 availableSpace, FixedPoint2 transferRate, float frameTime)
    {
        var flowScale = Math.Clamp(pressureDiff, 0f, 1f);
        var maxDrain = FixedPoint2.Min(availableSpace, transferRate * flowScale * frameTime);

        var taken = _solution.SplitSolution(puddleSol, maxDrain);
        _plumbing.InjectIntoNet(net, taken);

        maxDrain -= taken.Volume;

        // TODO: Find actual function that cleans up puddle?
        if (TryComp<SolutionComponent>(puddleSol, out var sol) && sol.Solution.Volume <= 0)
        {
            QueueDel(puddleSol);
            QueueDel(puddleUid);
        }
    }

    private void Backflow(PlumbingNet net, Entity<FluidPassiveVentComponent> ent, FixedPoint2 overflowAmount)
    {
        if (net.Liquid.Volume <= 0)
            return;

        if (overflowAmount <= 0)
        {
            if (net.ExternalPressureForce > 0.1f)
                overflowAmount = FixedPoint2.Min(net.Liquid.Volume, 1.0f);
            return;
        }

        var overflowSolution = net.Liquid.SplitSolution(overflowAmount);
        _puddle.TrySpillAt(ent.Owner, overflowSolution, out _);
    }
}
