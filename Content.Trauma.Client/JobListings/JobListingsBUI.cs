// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;
using JetBrains.Annotations;

namespace Content.Trauma.Client.JobListings;

[UsedImplicitly]
public sealed partial class JobListingsBUI : BoundUserInterface
{
    [ViewVariables]
    private JobListingsMenu? _menu;

    public JobListingsBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        var jobs = EntMan.System<JobListingsSystem>();
        if (jobs.GetJobBoard(owner) is not { } jobBoard)
            return;

        _menu = this.CreateWindow<JobListingsMenu>();
        _menu.OnAccepted += job => SendPredictedMessage(new JobListingsAcceptJobMessage(job));
        _menu.OnClaimed += job => SendPredictedMessage(new JobListingsClaimJobMessage(job));
        _menu.OnCancelled += job => SendPredictedMessage(new JobListingsCancelJobMessage(job));
        _menu.OnRefresh += () => SendPredictedMessage(new JobListingsRefreshMessage());
        _menu.Setup(jobs, jobBoard);
    }

    protected override void Open()
    {
        base.Open();
        _menu?.OpenCenteredLeft();
    }
}
