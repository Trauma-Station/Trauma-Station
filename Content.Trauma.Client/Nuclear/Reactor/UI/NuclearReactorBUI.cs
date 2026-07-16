// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Nuclear.Monitor;
using Content.Trauma.Shared.Nuclear.Reactor;

namespace Content.Trauma.Client.Nuclear.Reactor.UI;

/// <summary>
/// Initializes a <see cref="NuclearReactorWindow"/> and updates it when new server messages are received.
/// </summary>
public sealed class NuclearReactorBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private NuclearReactorWindow? _window;

    protected override void Open()
    {
        if (GetReactor(Owner, out var monitor) is not { } reactor)
            return;

        base.Open();

        _window = this.CreateWindow<NuclearReactorWindow>();
        _window.SetEntity(reactor, monitor);

        _window.OnSwapPart += pos => SendPredictedMessage(new ReactorSwapPartMessage(pos));
        _window.OnEjectItem += () => SendPredictedMessage(new ReactorEjectItemMessage());
        _window.OnAdjustControlRods += change => SendPredictedMessage(new ReactorAdjustControlRodsMessage(change));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is NuclearReactorBuiState cast)
            _window?.Update(cast);
    }

    private Entity<NuclearReactorComponent>? GetReactor(EntityUid uid, out EntityUid? monitor)
    {
        monitor = null;
        if (EntMan.TryGetComponent<NuclearReactorComponent>(uid, out var reactor))
            return (uid, reactor);

        if (EntMan.GetComponent<NuclearMonitorComponent>(uid).Linked is { } linked)
        {
            monitor = uid;
            return (linked, EntMan.GetComponent<NuclearReactorComponent>(linked));
        }

        return null;
    }
}
