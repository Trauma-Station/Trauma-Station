// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel;
using System.IO.Pipelines;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Shared.RetractableItemAction;
using Content.Trauma.Common.Plumbing;
using Content.Trauma.Server.Plumbing.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Plumbing;

public sealed partial class PlumbingSystem : CommonPlumbingSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly NodeGroupSystem _nodeGroup = default!;

    // A collection of all active plumbing networks
    private readonly List<PlumbingNet> _updateList = new();
    [ViewVariables]
    private readonly HashSet<PlumbingNet> _pipeNets = new();

    /// <summary>
    /// Adds a PlumbingNet to the processing list.
    /// Called by PlumbingNet.Initialize.
    /// </summary>
    [PublicAPI]
    public void AddPipeNet(PlumbingNet pipeNet)
    {
        _pipeNets.Add(pipeNet);
    }

    /// <summary>
    /// Removes a PlumbingNet. Called when a network is broken/deleted.
    /// </summary>
    [PublicAPI]
    public void RemovePipeNet(PlumbingNet pipeNet)
    {
        _pipeNets.Remove(pipeNet);

        // Raise an event similar to PipeNodeGroupRemovedEvent if other systems care
        var ev = new PlumbingNetRemovedEvent(pipeNet.Grid, pipeNet.NetId);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Updates all reagent networks.
    /// Replaces the Atmos 'AtmosTick' logic.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateDevices(frameTime);
        UpdateNets(frameTime);
    }

    private void UpdateDevices(float frameTime)
    {
        var ev = new PlumbingDeviceUpdateEvent(frameTime);
        var query = EntityQueryEnumerator<PlumbingDeviceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var device, out _))
        {
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void UpdateNets(float frameTime)
    {
        _updateList.Clear();
        _updateList.AddRange(_pipeNets);
        foreach (var pipeNet in _updateList)
        {
            // Ensure the net wasn't just deleted by a previous update in this tick
            if (_pipeNets.Contains(pipeNet))
                pipeNet.Update();
        }
    }

    /// <summary>
    /// Utility to merge a solution directly into a pipe network.
    /// Equivalent to MergeTileMixture in Atmos.
    /// </summary>
    [PublicAPI]
    public void InjectIntoNet(PlumbingNet net, Solution solution)
    {
        net.Liquid.AddSolution(solution, _proto);
    }

    /// <summary>
    ///     Removes a specific amount of solution from the pipe network.
    ///     Returns the extracted solution.
    /// </summary>
    [PublicAPI]
    public Solution RemoveFromNet(PlumbingNet net, FixedPoint2 amount)
    {
        // SplitSolution handles the math of taking a proportional
        // slice of every reagent currently in the 'Liquid' mix.
        return net.Liquid.SplitSolution(amount);
    }

    public override void UpdateNodeVisuals(EntityUid uid)
    {
        if (!TryComp<NodeContainerComponent>(uid, out var nodeContainer) || !TryComp<AtmosPipeLayersComponent>(uid, out var atmosPipe))
            return;

        // Update the layer values of all pipe nodes associated with the entity
        foreach (var (id, node) in nodeContainer.Nodes)
        {
            if (node is not PlumbingNode { } pipeNode)
                continue;

            if (pipeNode.CurrentPipeLayer == atmosPipe.CurrentPipeLayer)
                continue;

            pipeNode.CurrentPipeLayer = atmosPipe.CurrentPipeLayer;

            if (pipeNode.NodeGroup != null)
                _nodeGroup.QueueRemakeGroup((BaseNodeGroup) pipeNode.NodeGroup);
        }
    }

    public override bool IsPipeNode<T>(T node) { return node is PlumbingNode; }

    public override (PipeDirection, AtmosPipeLayer) GetAllDirectionsAndLayers<T>(Entity<TransformComponent> pipe, T node)
    {
        if (node is not PlumbingNode plumbingNode)
            throw new NotImplementedException();

        return (plumbingNode.OriginalPipeDirection.RotatePipeDirection(pipe.Comp.LocalRotation), plumbingNode.CurrentPipeLayer);
    }

    public override bool UpdateAppearance(EntityUid uid, ref HashSet<(EntityUid, AtmosPipeLayer)> connected)
    {
        NodeContainerComponent? container = null;
        bool anyPipeNodes = false;

        if (!Resolve(uid, ref container))
            return false;

        foreach (var node in container.Nodes.Values)
        {
            if (node is not PlumbingNode)
                continue;

            anyPipeNodes = true;

            foreach (var connectedNode in node.ReachableNodes)
            {
                if (connectedNode is PlumbingNode { } plumbingNode)
                    connected.Add((connectedNode.Owner, plumbingNode.CurrentPipeLayer));
            }
        }

        return anyPipeNodes;
    }
}
