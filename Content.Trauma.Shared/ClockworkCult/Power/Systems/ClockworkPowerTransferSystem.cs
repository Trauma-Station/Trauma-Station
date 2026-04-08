// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Popups;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
///
/// </summary>
public sealed class ClockworkPowerTransferSystem : EntitySystem
{
    [Dependency] private readonly SharedClockwinderSystem _clockwinder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityQuery<LimitedChargesComponent> _chargesQuery = default!;
    [Dependency] private readonly EntityQuery<ClockworkTransferrerComponent> _transferrerQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkTransferrerComponent, ClockwinderInteractEvent>(OnClockwinderInteract);
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

    #region Public Api

    /// <summary>
    /// Adds a new connection to the transferrer, and raises a <see cref="ClockworkConnectionEvent"/> event.
    /// </summary>
    public void AddConnection(Entity<ClockworkTransferrerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.Connections.Contains(target))
        {
            _popup.PopupPredicted("Already exists in the connections!", target, null, PopupType.SmallCaution);
            return;
        }

        // Disallow from transferring to same entity
        if (ent.Owner == target)
            return;

        // More than max connections, can't connect!
        if (ent.Comp.Connections.Count >= ent.Comp.MaxConnections)
        {
            // todo: loc it
            _popup.PopupPredicted("Too many connections! Can't connect.", target, null, PopupType.MediumCaution);
            return;
        }

        // todo: loc it
        _popup.PopupPredicted("Connection established.", target, null, PopupType.Medium);

        ent.Comp.Connections.Add(target);
        Dirty(ent);

        // Establish the active component so power is transferred to the connections.
        EnsureComp<ActiveClockworkTransferrerComponent>(ent.Owner);
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
    }

    /// <summary>
    /// Removes a clockwork structure from a transferrer
    /// </summary>
    public void RemoveConnection(Entity<ClockworkStructureComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.Transferrer is not {} transferrer)
            return;

        if (!_transferrerQuery.TryComp(transferrer, out var transfer))
            return;

        RemoveConnection((transferrer, transfer), ent.Owner);

        _popup.PopupPredicted("Removed connection from its network.", ent.Owner, null, PopupType.Medium);

        ent.Comp.Transferrer = null;
        Dirty(ent);
    }

    #endregion
}
