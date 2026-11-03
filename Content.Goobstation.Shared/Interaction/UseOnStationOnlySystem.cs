// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Interaction;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;

namespace Content.Goobstation.Shared.Interaction;

public sealed partial class UseOnStationOnlySystem : EntitySystem
{
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnUseAttempt(Entity<UseOnStationOnlyComponent> item, ref UseInHandAttemptEvent args)
    {
        if (_station.GetOwningStation(args.User) is not null)
            return;

        _popup.PopupEntity(Loc.GetString("use-on-station-only-not-on-station"), args.User, args.User);
        args.Cancelled = true;
    }
}
