// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
/// Handles the connection side of transferring
/// </summary>
public partial class ClockworkPowerTransferSystem
{
    [Dependency] private readonly EntityQuery<ClockworkConnectionHolderComponent> _connectionHolderQuery = default!;

    public void InitializeConnection()
    {
        SubscribeLocalEvent<ClockworkConnectionHolderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ClockworkConnectionHolderComponent, ClockworkConnectionAttemptEvent>(OnConnectionAttempt);
    }

    private void OnMapInit(Entity<ClockworkConnectionHolderComponent> ent, ref MapInitEvent args)
    {
        // Set the original connection in case we need it later.
        if (ent.Comp.OriginalConnection is {} connection)
            ent.Comp.OriginalConnection = connection;
        if (ent.Comp.TransferConnection is { } transferrer)
            ent.Comp.OriginalTransferConnection = transferrer;

        Dirty(ent);
    }

    private void OnConnectionAttempt(Entity<ClockworkConnectionHolderComponent> ent, ref ClockworkConnectionAttemptEvent args)
    {
        // If we don't have a set connection, then we can connect.
        if (ent.Comp.Connection is not {} ourConnection)
            return;

        // Okay, we match the connection type, so return and try to add us to the connections of the transferrer
        if (ourConnection == args.ConnectionRequired)
        {
            Log.Debug($"Same connection established {ourConnection.Id} and {args.ConnectionRequired}");
            return;
        }

        args.Cancelled = true;

        if (!_prototype.TryIndex(ourConnection, out var connection))
            return;

        // Here, we don't match the connection type, so show the user what connection we want.
        _popup.PopupPredicted($"This structure requires a {connection.ConnectionName} connection.", ent.Owner, null, PopupType.MediumCaution);
    }

    /// <summary>
    /// Sets a new <see cref="ClockworkConnectionPrototype"/> on the entity.
    /// </summary>
    public void SetConnection(Entity<ClockworkConnectionHolderComponent?> ent, ProtoId<ClockworkConnectionPrototype> newConnection)
    {
        if (!_connectionHolderQuery.Resolve(ent.Owner, ref ent.Comp) || !ent.Comp.AllowOverwrite)
            return;

        ent.Comp.Connection = newConnection;
        Dirty(ent);
    }

    /// <summary>
    /// Sets a new <see cref="ClockworkConnectionPrototype"/> on the entity (for transferring).
    /// </summary>
    public void SetTransferConnection(Entity<ClockworkConnectionHolderComponent?> ent, ProtoId<ClockworkConnectionPrototype> newConnection)
    {
        if (!_connectionHolderQuery.Resolve(ent.Owner, ref ent.Comp) || !ent.Comp.AllowOverwrite)
            return;

        ent.Comp.TransferConnection = newConnection;
        Dirty(ent);
    }

    /// <summary>
    /// Resets the <see cref="ClockworkConnectionPrototype"/> of the entity to the original one.
    /// </summary>
    public void ResetConnection(Entity<ClockworkConnectionHolderComponent?> ent)
    {
        if (!_connectionHolderQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Connection = ent.Comp.OriginalConnection;
        Dirty(ent);
    }

    /// <summary>
    /// Resets the <see cref="ClockworkConnectionPrototype"/> of the entity to the original one (for transferring).
    /// </summary>
    public void ResetTransferConnection(Entity<ClockworkConnectionHolderComponent?> ent)
    {
        if (!_connectionHolderQuery.Resolve(ent.Owner, ref ent.Comp) || ent.Comp.TransferConnection is null || !ent.Comp.AllowOverwrite)
            return;

        ent.Comp.TransferConnection = ent.Comp.OriginalTransferConnection;
        Dirty(ent);
    }
}
