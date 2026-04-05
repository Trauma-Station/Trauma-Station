// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Server.Pirates.Ransom;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Trauma.Shared.Plumbing;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Plumbing;

[NodeGroup(NodeGroupID.Fluid)] // Ensure you added this to the NodeGroupID enum
public sealed class PlumbingNet : BaseNodeGroup, IPlumbingNet
{
    [ViewVariables]
    public Solution Liquid { get; set; } = new();

    [ViewVariables]
    private PlumbingSystem? _plumbing;

    [ViewVariables]
    private ChemicalReactionSystem? _chemical;

    [ViewVariables]
    private PuddleSystem? _puddle;

    private IEntityManager? _entMan;

    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    /// Static pressure build-up from external sources (like puddles).
    /// </summary>
    public float ExternalPressureForce = 0;

    public EntityUid? Grid { get; private set; }

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        _entMan = entMan;

        Grid = _entMan.GetComponent<TransformComponent>(sourceNode.Owner).GridUid;

        _plumbing = _entMan.EntitySysManager.GetEntitySystem<PlumbingSystem>();
        _chemical = _entMan.EntitySysManager.GetEntitySystem<ChemicalReactionSystem>();
        _puddle = _entMan.EntitySysManager.GetEntitySystem<PuddleSystem>();

        _plumbing.AddPipeNet(this);
    }

    public void Update()
    {
        ExternalPressureForce = 0; // Resets the pressure so you don't get ghosting force and shit.

        if (Liquid.Contents.Count == 0 || Liquid.Volume == 0)
            return;

        var location = _random?.Pick(Nodes).Owner; // GOIDA

        _chemical?.FullyReactRaw(Liquid, location);
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        FixedPoint2 totalMaxVolume = 0;
        foreach (var node in groupNodes)
        {
            if (node is PlumbingNode reagentNode)
                totalMaxVolume += reagentNode.Volume;
        }
        Liquid.MaxVolume = totalMaxVolume;
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        if (Remaking)
            return;

        if (!node.Deleting)
            return;

        if (node is not PlumbingNode reagentNode || Liquid.Volume <= 0)
            return;

        var ratio = reagentNode.Volume / Liquid.MaxVolume;
        var spillAmount = Liquid.SplitSolution(Liquid.Volume * ratio);

        if (spillAmount.Volume > 0 && _entMan is { } entMan && entMan.TryGetComponent<TransformComponent>(node.Owner, out var xform) && xform is { } transform)
            _puddle?.TrySpillAt(transform.Coordinates, spillAmount, out _);

        Liquid.MaxVolume -= reagentNode.Volume;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        // Logic for splitting one big solution into smaller ones when a pipe is broken
        var survivors = new List<PlumbingNet>();
        var orphans = new List<PlumbingNode>();

        foreach (var group in newGroups)
        {
            if (group.Key is PlumbingNet net)
                survivors.Add(net);

            else if (group.Key is not { }) // This group has no network (it was deleted or disconnected)
            {
                foreach (var node in group)
                {
                    if (node is PlumbingNode pNode)
                        orphans.Add(pNode);
                }
            }
        }

        var totalVolume = Liquid.Volume;
        var totalCapacity = Liquid.MaxVolume;

        foreach (var orphan in orphans)
        {
            var shareRatio = totalCapacity > 0 ? (orphan.Volume / totalCapacity) : 0;
            var spillAmount = Liquid.SplitSolution(totalVolume * shareRatio);

            if (spillAmount.Volume > 0 && _entMan is { } && _entMan.TryGetComponent<TransformComponent>(orphan.Owner, out var xform))
                _puddle?.TrySpillAt(xform.Coordinates, spillAmount, out _);
        }

        if (survivors.Count > 0 && Liquid.Volume > 0)
        {
            var remainingCapacity = Liquid.MaxVolume;
            foreach (var net in survivors)
            {
                var netRatio = remainingCapacity > 0 ? (net.Liquid.MaxVolume / remainingCapacity) : 0;
                var share = Liquid.SplitSolution(Liquid.Volume * netRatio);
                net.Liquid.AddSolution(share, null);
            }
        }

        _plumbing?.RemovePipeNet(this);
        Removed = true;
    }
}
