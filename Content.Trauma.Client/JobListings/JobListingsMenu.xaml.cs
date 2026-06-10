// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : DefaultWindow
{
    public JobListingsMenu()
    {
        RobustXamlLoader.Load(this);
    }

    public void ClearJobListings()
    {
        AvailableJobListingsContainer.RemoveAllChildren();
    }

    public void AddAvailableSideJob(SideJobInfo sideJob)
    {
        var sideJobControl = new SideJobControl();
        sideJobControl.Update(sideJob);
        AvailableJobListingsContainer.AddChild(sideJobControl);
    }
}
