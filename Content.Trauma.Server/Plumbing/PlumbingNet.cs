// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Trauma.Shared.Plumbing;

namespace Content.Trauma.Server.Plumbing;

[NodeGroup(NodeGroupID.Reagent)] // Ensure you added this to the NodeGroupID enum
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
        if (Liquid.Contents.Count == 0 || Liquid.Volume == 0)
            return;

        var location = Nodes.FirstOrDefault()?.Owner;

        // Call your brand new API!
        _chemical?.FullyReactRaw(Liquid, location);
    }

    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        FixedPoint2 totalMaxVolume = 0;
        foreach (var node in groupNodes)
        {
            if (node is PlumbingNode reagentNode)
                totalMaxVolume += reagentNode.MaxVolume;
        }
        Liquid.MaxVolume = totalMaxVolume;
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        if (!node.Deleting || node is not PlumbingNode reagentNode || Liquid.Volume <= 0)
            return;

        if (Liquid.MaxVolume <= 0) return;

        // Remove the proportional amount of liquid that was inside this specific segment
        var ratio = reagentNode.MaxVolume / Liquid.MaxVolume;
        var spillAmount = Liquid.SplitSolution(Liquid.Volume * ratio);
        var xform = _entMan?.GetComponent<TransformComponent>(node.Owner);

        if (spillAmount.Volume > 0 && xform is { } transform)
            _puddle?.TrySpillAt(transform.Coordinates, spillAmount, out _);

        Liquid.MaxVolume -= reagentNode.MaxVolume;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        // Logic for splitting one big solution into smaller ones when a pipe is broken
        var newNets = new List<PlumbingNet>();
        foreach (var group in newGroups)
        {
            if (group.Key is PlumbingNet net)
                newNets.Add(net);
        }

        if (newNets.Count == 0) return;

        // Distribute reagents based on the volume capacity of the new pipe networks
        var totalMaxVol = Liquid.MaxVolume;
        var currentSolution = Liquid;

        foreach (var net in newNets)
        {
            var share = currentSolution.SplitSolution(currentSolution.Volume * (net.Liquid.MaxVolume / totalMaxVol));
            net.Liquid.AddSolution(share, null); // Re-inject into the new sub-net
        }
    }
}
