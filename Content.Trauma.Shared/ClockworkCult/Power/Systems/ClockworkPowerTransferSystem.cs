// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
///
/// </summary>
public sealed partial class ClockworkPowerTransferSystem : EntitySystem
{
    [Dependency] private readonly SharedClockwinderSystem _clockwinder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityQuery<ClockworkStructureComponent> _structureQuery = default!;
    [Dependency] private readonly EntityQuery<LimitedChargesComponent> _chargesQuery = default!;
    [Dependency] private readonly EntityQuery<ClockworkTransferrerComponent> _transferrerQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeConnection();

        SubscribeLocalEvent<ClockworkTransferrerComponent, ClockwinderInteractEvent>(OnClockwinderInteract);
        SubscribeLocalEvent<ClockworkTransferrerComponent, ClockworkConnectionEstablishedEvent>(OnConnectionEstablished);
        SubscribeLocalEvent<ClockworkTransferrerComponent, ClockworkConnectionDisconnectedEvent>(OnConnectionDisconnected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // TODO: Optimize this more?
        var now = _timing.CurTime;
        var eqe = EntityQueryEnumerator<ActiveClockworkTransferrerComponent, ClockworkTransferrerComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var transferrer))
        {
            if (now < active.NextUpdate)
                continue;

            if (!_chargesQuery.TryComp(uid, out var chargesTransferrer))
                continue;

            // The current active connections
            var activeConnections = transferrer.Connections.Count;
            if (activeConnections == 0) // should never happen
                continue;

            var chargesPerConnection = transferrer.Transfer / activeConnections;    // The charge to give per connection active
            var totalChargesToRemove = -(transferrer.Transfer * activeConnections); // The total charges to remove from us

            // Check if we can transfer
            var ourCharges = _charges.GetCurrentCharges((uid, chargesTransferrer));
            if (ourCharges + totalChargesToRemove <= 0)
                return;

            // Remove the charges from us, based on the amount of connections
            _charges.AddCharges((uid,chargesTransferrer), totalChargesToRemove);
            Log.Debug($"Removed {totalChargesToRemove} from: {uid.Id}.");

            // Add the charges we removed to the connections
            foreach (var structure in transferrer.Connections)
            {
                _charges.AddCharges(structure, chargesPerConnection - transferrer.TransferLossPerConnection);
                Log.Debug($"Added {chargesPerConnection} to: {structure.Id}. " +
                          $"Lost {transferrer.TransferLossPerConnection} charges.");
            }

            active.NextUpdate = now + transferrer.UpdateInterval;
            Dirty(uid, active);
        }
    }

    private void OnClockwinderInteract(Entity<ClockworkTransferrerComponent> ent, ref ClockwinderInteractEvent args)
    {
        // Either overrides the current transferrer, or sets it if there isn't one
        _clockwinder.SetTransferrer(args.Clockwinder,  ent.Owner);
    }

    private void OnConnectionEstablished(Entity<ClockworkTransferrerComponent> ent, ref ClockworkConnectionEstablishedEvent args)
    {
        // If we are a transferrer, and another transferrer connects to us,
        // then our transfer connection type will change to the transferrer's.
        //
        // This ensures that structures like Clockwork Connectors that have 1 connection slot only, and transfer a unique connection type,
        // will change the transfer connection of a let's say, clockwork pole

        // Try to clear all the other connections of different type, we don't want to
        SetTransferConnection(ent.Owner, args.ConnectionPassed);

        if (!_structureQuery.TryComp(ent.Owner, out var structure))
            return;

        // Remove anyone transferring to this machine of different type
        foreach (var entity in structure.Transferrers)
        {
            if (entity is not { } transfer)
                continue;

            if (!_connectionHolderQuery.TryComp(transfer, out var connection) || connection.TransferConnection == args.ConnectionPassed)
                continue;

            // Remove us from their connections if we don't match
            RemoveConnection(transfer, ent.Owner);
        }
    }

    private void OnConnectionDisconnected(Entity<ClockworkTransferrerComponent> ent, ref ClockworkConnectionDisconnectedEvent args)
    {
        // If we are a transferrer, and another transferrer disconnects from us,
        // then our transfer connection type will reset to its original transfer connection.
        //
        // For example, if we have an X type connected to us, and we originally had Y type on transferring,
        // then after we get disconnected from them, we get our Y type back.
        ResetTransferConnection(ent.Owner);
    }

    #region Public Api

    /// <summary>
    /// Adds a new connection to the transferrer, and raises a <see cref="ClockworkConnectionEvent"/> event.
    /// </summary>
    public void AddConnection(Entity<ClockworkTransferrerComponent?, ClockworkConnectionHolderComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        if (ent.Comp2.TransferConnection is not { } connRequired)
            return;

        // We want to check here that our transferrer has a valid connection to connect to our target
        var attemptEv = new ClockworkConnectionAttemptEvent(connRequired);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        if (ent.Comp1.Connections.Contains(target))
        {
            _popup.PopupPredicted("Already exists in the connections!", target, null, PopupType.SmallCaution);
            return;
        }

        // Disallow from transferring to same entity
        if (ent.Owner == target)
            return;

        // More than max connections, can't connect!
        if (ent.Comp1.Connections.Count >= ent.Comp1.MaxConnections)
        {
            _popup.PopupPredicted("Too many connections! Can't connect.", target, null, PopupType.MediumCaution);
            return;
        }

        _popup.PopupPredicted("Connection established.", target, null, PopupType.Medium);

        ent.Comp1.Connections.Add(target);
        Dirty(ent, ent.Comp1);

        // Establish the active component so power is transferred to the connections.
        EnsureComp<ActiveClockworkTransferrerComponent>(ent.Owner);

        // Notify the target for anything
        if (ent.Comp2.TransferConnection is not { } transferConnection)
            return;

        var ev = new ClockworkConnectionEstablishedEvent(transferConnection);
        RaiseLocalEvent(target, ref ev);
    }

    /// <summary>
    /// Removes a connection from the transferrer
    /// </summary>
    public void RemoveConnection(Entity<ClockworkTransferrerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Connections.Remove(target);
        Dirty(ent);

        // Stop updating if we don't have any connections
        if (ent.Comp.Connections.Count <= 0)
            RemCompDeferred<ActiveClockworkTransferrerComponent>(ent.Owner);

        var ev = new ClockworkConnectionDisconnectedEvent();
        RaiseLocalEvent(target, ref ev);
    }

    /// <summary>
    /// Removes a clockwork structure from a transferrer
    /// </summary>
    public void RemoveConnection(Entity<ClockworkStructureComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        List<EntityUid> toRemove = new();
        foreach (var transferrerEnt in ent.Comp.Transferrers)
        {
            if (transferrerEnt is not {} transferrer
                || !_transferrerQuery.TryComp(transferrer, out var transfer))
                continue;

            RemoveConnection((transferrer, transfer), ent.Owner);

            toRemove.Add(transferrer);
        }

        // Remove all the transferrers
        foreach (var entity in toRemove)
        {
            ent.Comp.Transferrers.Remove(entity);
            Dirty(ent);
        }

        _popup.PopupPredicted("Removed connection from its network.", ent.Owner, null, PopupType.Medium);
    }

    #endregion
}

/// <summary>
/// An attempt event raised on the entity we want to connect to our transferrer, before establishing a connection.
/// </summary>
[ByRefEvent]
public record struct ClockworkConnectionAttemptEvent(
    ProtoId<ClockworkConnectionPrototype> ConnectionRequired,
    bool Cancelled = false);

/// <summary>
/// Raised on the target once a connection is established.
/// </summary>
[ByRefEvent]
public record struct ClockworkConnectionEstablishedEvent(ProtoId<ClockworkConnectionPrototype> ConnectionPassed);

/// <summary>
/// Raised on the target once a connection is removed.
/// </summary>
[ByRefEvent]
public record struct ClockworkConnectionDisconnectedEvent;
