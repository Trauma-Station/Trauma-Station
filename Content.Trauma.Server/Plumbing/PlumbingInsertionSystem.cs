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

namespace Content.Trauma.Server.Plumbing;

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

        if (!container.Nodes.TryGetValue("pipe", out var node) || node is not PlumbingNode pNode || pNode.PipeNet is not PlumbingNet net)
            return;

        var availableSpace = net.Liquid.MaxVolume - net.Liquid.Volume;

        if (availableSpace < 0)
        {
            var overflowAmount = net.Liquid.Volume - net.Liquid.MaxVolume;
            var overflowSolution = net.Liquid.SplitSolution(overflowAmount);
            _puddle.TrySpillAt(uid, overflowSolution, out _);
        }

        if (availableSpace > 0)
        {
            var maxDrain = FixedPoint2.Min(availableSpace, vent.TransferRate * args.FrameTime);

            if (!TryComp(uid, out TransformComponent? xform) || xform.GridUid is not { })
                return;

            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
                return;

            var tile = _map.GetTileRef((xform.GridUid.Value, grid), xform.Coordinates);

            if (!_puddle.TryGetPuddle(tile, out var puddleUid) || !TryComp<PuddleComponent>(puddleUid, out var puddleComp) || puddleComp.Solution is not { } puddleSol)
                return;

            var taken = _solution.SplitSolution(puddleSol, maxDrain);

            if (taken.Volume <= 0)
                return;

            // Inject the drained puddle into the pipe network.
            _plumbing.InjectIntoNet(net, taken);

            // Update remaining capacity for this tick in case there are multiple puddles.
            maxDrain -= taken.Volume;
            if (TryComp<SolutionComponent>(puddleSol, out var sol) && sol.Solution.Volume <= 0)
            {
                QueueDel(puddleSol);
                QueueDel(puddleUid);
            }
        }
    }
}
