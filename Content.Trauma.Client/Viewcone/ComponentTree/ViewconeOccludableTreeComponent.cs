// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Viewcone.Components;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Trauma.Client.Viewcone.ComponentTree;

[RegisterComponent]
public sealed partial class ViewconeOccludableTreeComponent : Component, IComponentTreeComponent<ViewconeOccludableComponent>
{
    public DynamicTree<ComponentTreeEntry<ViewconeOccludableComponent>> Tree { get; set; }
}
