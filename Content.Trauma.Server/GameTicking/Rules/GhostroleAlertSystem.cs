// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared.GameTicking.Components;
using Content.Shared.Ghost;
using Content.Trauma.Server.Ghost;
using Content.Trauma.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed class GhostroleAlertSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostroleAlertComponent, GameRuleAddedEvent>(OnRuleAdded);
    }

    private void OnRuleAdded(Entity<GhostroleAlertComponent> ent, ref GameRuleAddedEvent args)
    {
        var query = EntityQueryEnumerator<GhostComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out var actor))
        {
            _euiMan.OpenEui(new GhostroleAlertEui(), actor.PlayerSession);
        }
    }
}
