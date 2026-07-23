// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Dragon;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Shared.Destructible;
using Content.Shared.GameTicking;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

public sealed partial class TraumaDragonRuleSystem : EntitySystem
{
    [Dependency] private RoundEndSystem _roundEnd = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DragonRiftComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DragonComponent>(ent.Comp.Dragon, out var dragon))
            return;

        if (dragon.Rifts.Count > 2)
            _roundEnd.RequestRoundEnd(countdownTime: TimeSpan.FromMinutes(5));
    }
}
