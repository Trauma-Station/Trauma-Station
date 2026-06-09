// SPDX-License-Identifier: AGPL-3.0-or-later

using JetBrains.Annotations;

namespace Content.Trauma.Client.JobListings;

[UsedImplicitly]
public sealed class JobListingsBoundUserInterface : BoundUserInterface
{
    public JobListingsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    [ViewVariables]
    private JobListingsMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindowCenteredLeft<JobListingsMenu>();
    }
}
