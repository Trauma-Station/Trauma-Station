// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Trauma.Shared.Plumbing;

public interface IPlumbingNet : INodeGroup, ISolutionMixtureHolder
{
    /// <summary>
    /// Causes reagents in the PlumbingNet to react.
    /// </summary>
    void Update();
}
