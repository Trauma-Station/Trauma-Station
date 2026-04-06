// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Attribute.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Trauma.Shared.Attribute.Components;

/// <summary>
/// Contains attribute entities inside with <see cref="AttributeComponent"/>.
/// Assigned to some physical bodies, for example brains.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class AttributeContainerComponent : Component
{
    public const string ContainerId = "attribute";

    /// <summary>
    /// The actual container that contains all attribute entities.
    /// </summary>
    [ViewVariables]
    public Container? Container;

    /// <summary>
    /// The attribute holder using this container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Holder;

    /// <summary>
    /// Contains a dictionary of prototypes to attribute entities, which are stored inside <see cref="AttributeContainer"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> AttributeDict = new();

}
