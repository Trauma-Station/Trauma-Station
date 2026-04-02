// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Used on clockwork structures that can transfer their power charges to entities with <see cref="ClockworkStructureComponent"/>,
/// via an entity with <see cref="ClockwinderComponent"/>.
///
/// TODO: Add new charges api for clockwork cause ts not gonna work if this keeps up :sob: :pray:
///
/// Make sure your entity has <see cref="LimitedChargesComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkTransferrerComponent : Component
{
    /// <summary>
    /// The maximum amount of connections this entity can have,
    /// </summary>
    [DataField]
    public int MaxConnections = 5;

    /// <summary>
    /// The active connections this entity has
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Connections = new();
}
