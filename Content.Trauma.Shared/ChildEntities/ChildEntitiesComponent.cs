// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.ChildEntities;

/// <summary>
/// Spawns entities parented to the entity this component is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChildEntitiesComponent : Component
{
    /// <summary>
    /// The entities to spawn attached to the component owner
    /// </summary>
    [DataField(required: true)]
    public List<ChildEntityInfo> ChildPrototypes = new();

    /// <summary>
    /// How many children we have active, so we can discard them after the component shutdowns
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Children = new();
}

/// <summary>
/// Holds the <see cref="EntProtoId"/> and the offset of the entity we want to attach
/// </summary>
[Serializable, NetSerializable, DataRecord]
public partial record struct ChildEntityInfo(EntProtoId Prototype, Vector2 Offset);
