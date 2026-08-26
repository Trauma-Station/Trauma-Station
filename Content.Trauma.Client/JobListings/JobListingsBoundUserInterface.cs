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
        _menu.OnAccepted += OnAccepted;
        _menu.OnClaimed += OnClaimed;
        _menu.OnCancelled += OnCancelled;
        _menu.OnRefresh += OnRefresh;
    }

    [ViewVariables]
    private JobListingsMenu? _menu;

    public override void Update()
    {
        base.Update();
        _menu?.Update(Owner);
    }

    protected override void Open()
    {
        base.Open();
        _menu?.OpenCenteredLeft();
    }

    private void OnAccepted(NetEntity job)
    {
        SendMessage(new JobListingsAcceptJobMessage(job));
    }

    private void OnClaimed(NetEntity job)
    {
        SendMessage(new JobListingsClaimJobMessage(job));
    }

    private void OnCancelled(NetEntity job)
    {
        SendMessage(new JobListingsCancelJobMessage(job));
    }

    private void OnRefresh()
    {
        SendMessage(new JobListingsRefreshMessage());
    }
}
