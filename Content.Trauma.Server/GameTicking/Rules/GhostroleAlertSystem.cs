// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost.Components;
using Content.Trauma.Server.Ghost;
using Content.Trauma.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class GhostroleAlertSystem : EntitySystem
{
    [Dependency] private EuiManager _eui = default!;

    [SubscribeLocalEvent]
    private void OnRuleAdded(Entity<GhostroleAlertComponent> ent, ref GameRuleAddedEvent args)
    {
        var query = EntityQueryEnumerator<GhostComponent, ActorComponent>();
        foreach (var ghost in query)
        {
            _eui.OpenEui(new GhostroleAlertEui(), ghost.Comp2.PlayerSession);
        }
    }
}
