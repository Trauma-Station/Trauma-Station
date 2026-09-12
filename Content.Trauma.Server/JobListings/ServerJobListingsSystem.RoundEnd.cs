// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Server.Objectives;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// System that manages the round end summary for job listings.
/// Says how much reputation each player aquired, how maby jobs they did, and what their title was.
/// </summary>
public sealed partial class ServerJobListingsSystem
{
    [Dependency] private ObjectivesSystem _objectives = default!;

    private readonly StringBuilder _sb = new StringBuilder();

    [SubscribeLocalEvent]
    private void OnPrependObjectives(Entity<RoundEndJobListingsInfoComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        _sb.Clear();

        var query = EntityQueryEnumerator<JobListingsComponent>();
        while (query.MoveNext(out var uid, out var jobBoard))
        {
            if (jobBoard.Mind is not { } mind)
                return;

            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            var name = _objectives.GetTitle((mind, mindComp), Name(mindComp.OwnedEntity ?? mind));
            var level = GetReputationLevel((uid, jobBoard));
            var title = Loc.GetString($"job-listings-ui-reputation-level-{level}");
            _sb.AppendLine(Loc.GetString("job-listings-round-end", ("name", name), ("count", jobBoard.JobsCompleted), ("reputation", jobBoard.Reputation), ("title", title)));
        }

        args.Text = _sb.ToString();
    }
}
