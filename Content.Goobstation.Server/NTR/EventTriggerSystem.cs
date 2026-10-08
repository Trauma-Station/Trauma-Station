// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;

namespace Content.Goobstation.Server.NTR;

public sealed partial class EventTriggerSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, EventTriggerComponent component, MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(component.EventId))
            _ticker.StartGameRule(component.EventId, out _);
    }
}
