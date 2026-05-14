// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Content.Trauma.Common.TileMovement;

namespace Content.Lavaland.Shared.Movement;

public sealed partial class HierophantBeatSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantBeatComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HierophantBeatComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<HierophantBeatComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnStartup(EntityUid uid, HierophantBeatComponent component, ref ComponentStartup args)
    {
        EnsureComp<TileMovementComponent>(uid);
        _alertsSystem.ShowAlert(uid, component.HierophantBeatAlertId);
    }

    private void OnRemove(EntityUid uid, HierophantBeatComponent component, ref ComponentRemove args)
    {
        RemComp<TileMovementComponent>(uid);
        _alertsSystem.ClearAlert(uid, component.HierophantBeatAlertId);
    }

    private void OnRefreshSpeed(EntityUid uid, HierophantBeatComponent component, ref RefreshMovementSpeedModifiersEvent args)
        => args.ModifySpeed(component.MovementSpeedBuff, component.MovementSpeedBuff);
}
