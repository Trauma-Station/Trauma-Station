// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Used on clockwork structures that can transfer their power charges to entities with <see cref="ClockworkStructureComponent"/>,
/// via an entity with <see cref="ClockwinderComponent"/>.
///
/// Make sure your entity has <see cref="LimitedChargesComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkTransferrerComponent : Component
{
    /// <summary>
    /// The maximum amount of connections this entity can have.
    /// </summary>
    [DataField]
    public int MaxConnections = 5;

    /// <summary>
    /// How often to send power to the <see cref="Connections"/>.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How many charges are lost while transferring.
    ///
    /// Decreases the <see cref="Transfer"/> variable, when doing transfers.
    /// </summary>
    [DataField]
    public int TransferLossPerConnection;

    /// <summary>
    /// How many base charges to transfer every update, to each connection.
    /// </summary>
    [DataField]
    public int Transfer = 10;

    /// <summary>
    /// The active connections this entity has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Connections = new();
}

/// <summary>
/// Active version for use in update loops.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ActiveClockworkTransferrerComponent :Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate;
}
