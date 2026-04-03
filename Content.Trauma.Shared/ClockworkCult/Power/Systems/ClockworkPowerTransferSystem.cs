// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Power.Systems;

/// <summary>
///
/// </summary>
public sealed class ClockworkPowerTransferSystem : EntitySystem
{
    [Dependency] private readonly ClockwinderSystem _clockwinder = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClockworkTransferrerComponent, ClockwinderInteractEvent>(OnClockwinderInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // TODO: Implement active component
    }

    private void OnClockwinderInteract(Entity<ClockworkTransferrerComponent> ent, ref ClockwinderInteractEvent args)
    {
        // Either overrides the current transferrer, or sets it if there isn't one
        _clockwinder.SetTransferrer(args.Clockwinder,  ent.Owner);
        args.Handled = true; // Handle here, since it may have ClockworkStructureComponent too
    }

    #region Public Api

    /// <summary>
    /// Adds a new connection to the transferrer, and raises a <see cref="ClockworkConnectionEvent"/> event.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="target"></param>
    public void AddConnection(Entity<ClockworkTransferrerComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
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

        // Raise event here to update the charge output
        var ev = new ClockworkConnectionEvent();
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    #endregion
}

/// <summary>
/// Raised on the transferrer to update its power production.
/// </summary>
[ByRefEvent]
public record struct ClockworkConnectionEvent;
