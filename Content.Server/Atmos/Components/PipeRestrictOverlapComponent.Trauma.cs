using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.Atmos.Components;
public sealed partial class PipeRestrictOverlapComponent
{
    [DataField]
    public NodeGroupID Group = NodeGroupID.Pipe;
}
