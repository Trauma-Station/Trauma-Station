// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Server.StationEvents.Events;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles.Jobs;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.StationEvents;

public sealed partial class JobAddCollectiveMindRule : StationEventSystem<JobAddCollectiveMindRuleComponent>
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedJobSystem _job = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    // TODO: delete this and add collective mind entity effect
    protected override void Started(EntityUid uid, JobAddCollectiveMindRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var target, out var mindContainer))
        {
            if (mindContainer.Mind is not {} mind)
                continue;

            foreach (var proto in component.Affected)
            {
                if (!_job.MindHasJobWithId(mind, proto))
                    continue;

                EnsureComp<CollectiveMindComponent>(target).Channels.Add(component.Channel);
                if (component.Message != null &&
                    TryComp<MindComponent>(mind, out var mindComp) &&
                    _player.TryGetSessionById(mindComp.UserId, out var session))
                {
                    var message = Loc.GetString("chat-manager-server-wrap-message", ("message", Loc.GetString(component.Message)));
                    _chat.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false, session.Channel);
                }
                break;
            }
        }
    }
}
