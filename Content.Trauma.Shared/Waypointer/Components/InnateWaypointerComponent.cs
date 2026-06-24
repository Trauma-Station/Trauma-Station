// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Waypointer.Components;

/// <summary>
///  This is used for entities that have an innate waypointer.
/// </summary>
/// <example>
/// Dragons.
/// </example>
[RegisterComponent, NetworkedComponent]
public sealed partial class InnateWaypointerComponent: Component
{
    /// <summary>
    /// The prototype of the waypointer that this entity will have.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<WaypointerPrototype>> WaypointerProtoIds = default!;
}
