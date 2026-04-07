// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Trauma.Server.StationEvents.Components;
using Robust.Shared.Player;

namespace Content.Trauma.Server.StationEvents.Events;

public sealed class CarpMigrationRule : StationEventSystem<CarpMigrationRuleComponent>
{
    protected override void Started(EntityUid uid, CarpMigrationRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var filter = Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame);
        ChatSystem.DispatchFilteredAnnouncement(filter,
            "Unknown biological entities have been detected near the station, please stand by.",
            sender: "Lifesign Alert",
            colorOverride: Color.Gold);
    }
}
