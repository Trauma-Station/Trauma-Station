using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Trauma.Shared.Plumbing;

public interface IPlumbingNet : INodeGroup, ISolutionMixtureHolder
{
    /// <summary>
    /// Causes reagents in the PlumbingNet to react.
    /// </summary>
    void Update();
}
