// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : DefaultWindow
{
    public Action<NetEntity>? OnJobAccepted;

    public JobListingsMenu()
    {
        RobustXamlLoader.Load(this);
    }

    public void ClearJobListings()
    {
        AvailableJobListingsContainer.RemoveAllChildren();
        AcceptedJobListingsContainer.RemoveAllChildren();
    }

    public void AddAvailableSideJob(SideJobInfo sideJob)
    {
        var sideJobControl = new SideJobControl();
        sideJobControl.UpdateAsAvailable(sideJob);
        sideJobControl.OnAccepted += job => OnJobAccepted?.Invoke(job);
        AvailableJobListingsContainer.AddChild(sideJobControl);
        RefreshNotes();
    }

    public void AddAcceptedSideJob(SideJobInfo sideJob)
    {
        var sideJobControl = new SideJobControl();
        sideJobControl.UpdateAsAccepted(sideJob);
        AcceptedJobListingsContainer.AddChild(sideJobControl);
        RefreshNotes();
    }

    public void DisableAcceptButtons()
    {
        foreach (var control in AvailableJobListingsContainer.Children)
        {
            if (control is not SideJobControl sideJobControl)
                continue;
            sideJobControl.JobListingPositiveButton.Disabled = true;
        }
    }

    private void RefreshNotes()
    {
        AcceptedJobListingsNote.Visible = AcceptedJobListingsContainer.ChildCount == 0;
        AvailableJobListingsNote.Visible = AvailableJobListingsContainer.ChildCount == 0;
    }
}
