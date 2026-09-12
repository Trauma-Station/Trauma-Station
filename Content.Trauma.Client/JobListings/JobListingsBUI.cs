// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using JetBrains.Annotations;

namespace Content.Trauma.Client.JobListings;

[UsedImplicitly]
public sealed partial class JobListingsBUI : BoundUserInterface
{
    [ViewVariables]
    private JobListingsMenu? _menu;

    public JobListingsBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _menu = this.CreateWindow<JobListingsMenu>();
        _menu.OnAccepted += job => SendPredictedMessage(new JobListingsAcceptJobMessage(job));
        _menu.OnClaimed += job => SendPredictedMessage(new JobListingsCancelJobMessage(job));
        _menu.OnCancelled += job => SendPredictedMessage(new JobListingsCancelJobMessage(job));
        _menu.OnRefresh += () => SendPredictedMessage(new JobListingsRefreshMessage());
        _menu.SetOwner(Owner);
    }

    protected override void Open()
    {
        base.Open();
        _menu?.OpenCenteredLeft();
    }
}
