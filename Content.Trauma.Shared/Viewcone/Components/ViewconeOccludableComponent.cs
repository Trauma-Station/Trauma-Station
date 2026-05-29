// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Trauma.Shared.Viewcone.Components;

/// <summary>
/// Marks an entity as one which should fade away clientside if you have a viewcone and it's out of view
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewconeOccludableComponent : Component, IComponentTreeEntry<ViewconeOccludableComponent>
{
    /// <summary>
    /// If true, prevents being hidden if this is anchored.
    /// Useful for unanchorable structures so they can hide if being moved.
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
    /// If true, viewcone alpha handling will always override the base alpha of this entity when setting transparency.
    /// Useful for viewcone effects.
    /// </summary>
    [DataField]
    public bool OverrideBaseAlpha;

    /// <summary>
    /// If this is a temporary entity (like an effect), then this is the originating player (or other source)
    /// of this occludable.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    /// <summary>
    /// Clientside memory entity that gets spawned when this thing exits your vision cone, if <see cref="UseMemory"/> is true.
    /// </summary>
    [ViewVariables]
    public EntityUid? Memory;

    /// <summary>
    /// Whether to spawn a memory entity or fade this entity out as it exits your vision cone.
    /// Useful to disable for sound effect indicators.
    /// </summary>
    [DataField]
    public bool UseMemory = true;

    /// <summary>
    /// When this entity was last in the client's vision cone, used for fading away.
    /// </summary>
    public TimeSpan LastSeen;

    // Clientside comptree stuff
    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<ViewconeOccludableComponent>>? Tree { get; set; }
    public bool AddToTree => true;
    public bool TreeUpdateQueued { get; set; }
}
