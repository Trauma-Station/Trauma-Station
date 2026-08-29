// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.JobListings;

[UsedImplicitly]
public sealed partial class JobListingsBoundUserInterface : BoundUserInterface
{
    [Dependency] private IGameTiming _timing = default!;

    public JobListingsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _menu = this.CreateWindow<JobListingsMenu>();
        _menu.OnAccepted += OnAccepted;
        _menu.OnClaimed += OnClaimed;
        _menu.OnCancelled += OnCancelled;
        _menu.OnRefresh += OnRefresh;
    }

    [ViewVariables]
    private JobListingsMenu? _menu;

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not JobListingsBoundUserInterfaceState jobState)
            return;
        if (_menu is null)
            return;

        _menu.ClearJobListings();

        foreach (var sideJob in jobState.AvailableSidejobs)
        {
            _menu.AddAvailableSideJob(sideJob);
        }

        foreach (var sideJob in jobState.AcceptedSideJobs)
        {
            _menu.AddAcceptedSideJob(sideJob);
        }

        _menu.SetReputation(jobState.Reputation, jobState.ReputationLevel);
        _menu.SetRefresh(jobState.BonusRefresh, jobState.RefreshTime, jobState.RefreshWaitDuration);
        _menu.Refresh(jobState.MaximumAcceptedSideJobs, jobState.Loading);
    }

    protected override void Open()
    {
        base.Open();
        _menu?.OpenCenteredLeft();
    }

    private void OnAccepted(NetEntity job)
    {
        SendPredictedMessage(new JobListingsAcceptJobMessage(job));
    }

    private void OnClaimed(NetEntity job)
    {
        SendPredictedMessage(new JobListingsClaimJobMessage(job));
    }

    private void OnCancelled(NetEntity job)
    {
        SendPredictedMessage(new JobListingsCancelJobMessage(job));
    }

    private void OnRefresh()
    {
        SendPredictedMessage(new JobListingsRefreshMessage());
    }
}
