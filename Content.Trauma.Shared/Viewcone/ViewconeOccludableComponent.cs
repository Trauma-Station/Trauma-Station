// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Wall;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Marks an entity as one which should fade away clientside if you have a viewcone and it's out of view
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ViewconeOccludableComponent : Component, IComponentTreeEntry<ViewconeOccludableComponent>
{
    /// <summary>
    /// If set to true, only allows occluding if this entity is anchored.
    /// </summary>
    [DataField]
    public bool OccludeIfAnchored;

    /// <summary>
    /// Whether the occluding should be inverted,
    /// i.e. the sprite will be invisible while within view, and visible outside of view
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Inverted;

    /// <summary>
    /// If this is a temporary entity (like an effect), then this is the originating player (or other source)
    /// of this occludable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    // Clientside comptree stuff
    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<ViewconeOccludableComponent>>? Tree { get; set; }
    public bool AddToTree => true;
    public bool TreeUpdateQueued { get; set; }
}
