// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using JetBrains.Annotations;

namespace Content.Trauma.Client.JobListings;

[UsedImplicitly]
public sealed class JobListingsBoundUserInterface : BoundUserInterface
{
    public JobListingsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _menu = this.CreateWindow<JobListingsMenu>();
        _menu.OnJobAccepted += OnJobAccepted;
        _menu.OnJobClaimed += OnJobClaimed;
        _menu.OnJobCancelled += OnJobCancelled;
        _menu.OnRefresh += OnRefresh;
    }

    [ViewVariables]
    private JobListingsMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu?.OpenCenteredLeft();
    }

    private void OnJobAccepted(NetEntity job)
    {
        SendMessage(new JobListingsAcceptJobMessage(job));
    }

    private void OnJobClaimed(NetEntity job)
    {
        SendMessage(new JobListingsClaimJobMessage(job));
    }

    private void OnJobCancelled(NetEntity job)
    {
        SendMessage(new JobListingsCancelJobMessage(job));
    }

    private void OnRefresh()
    {
        SendMessage(new JobListingsRefreshMessage());
    }
}
