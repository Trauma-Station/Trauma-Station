// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Power;

/// <summary>
/// This is a prototype for the types of connections an entity with <see cref="ClockworkStructureComponent"/> can accept.
///
/// For example, one structure will not initiate a connection, if the connection type does not match with the required one.
///
/// An entity with <see cref="ClockworkTransferrerComponent"/> stores the connection type that it will send to entities
/// with <see cref="ClockworkStructureComponent"/>.
/// </summary>
[Prototype]
public sealed partial class ClockworkConnectionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of this connection,
    /// to show to the user in case they need to know which connection is required.
    /// </summary>
    [DataField("name")]
    public string ConnectionName = string.Empty;

    /// <summary>
    /// The color of this connection to show when a client has the <see cref="ClockworkTransferOverlay"/> activated.
    /// </summary>
    [DataField("color")]
    public Color ConnectionColor = Color.Gold;
}
