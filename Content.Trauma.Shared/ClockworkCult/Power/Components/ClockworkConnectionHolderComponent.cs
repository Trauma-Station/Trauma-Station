// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// A simple holder component that holds the connection this entity can transfer or connect to.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkConnectionHolderComponent : Component
{
    /// <summary>
    /// The connection which we accept for other transferrers to connect to us.
    ///
    /// If null, any connection type can connect to this structure.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ClockworkConnectionPrototype>? Connection;

    /// <summary>
    /// The connection which we transfer.
    ///
    /// Requires <see cref="ClockworkTransferrerComponent"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ClockworkConnectionPrototype>? TransferConnection;

    /// <summary>
    /// Whether to allow overwritting the connections
    /// </summary>
    [DataField]
    public bool AllowOverwrite = true;

    [AutoNetworkedField]
    public ProtoId<ClockworkConnectionPrototype> OriginalConnection;

    [AutoNetworkedField]
    public ProtoId<ClockworkConnectionPrototype> OriginalTransferConnection;
}
