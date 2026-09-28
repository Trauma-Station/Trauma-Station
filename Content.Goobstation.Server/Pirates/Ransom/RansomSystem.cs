// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Pirates;
using Content.Goobstation.Server.Pirates.GameTicking.Rules;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Goobstation.Server.Pirates.Ransom;

public sealed partial class RansomSystem : EntitySystem
{
    [Dependency] private PendingPirateRuleSystem _pprs = default!;
    [Dependency] private GameTicker _ticker = default!;

    [SubscribeLocalEvent]
    private void OnGetRansom(Entity<RansomComponent> ent, ref ComponentStartup args)
    {
        var eqe = EntityQueryEnumerator<PendingPirateRuleComponent, GameRuleComponent>();
        while (eqe.MoveNext(out var uid, out var prule, out var gamerule))
        {
            _ticker.EndGameRule((uid, gamerule));
            _pprs.SendAnnouncement((uid, prule), PendingPirateRuleSystem.AnnouncementType.Paid);
        }
    }
}
