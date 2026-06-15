// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : DefaultWindow
{
    public Action<EntityUid>? OnJobAccepted;

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
        sideJobControl.OnAccepted += job => OnJobAccepted?.Invoke(job);
        AvailableJobListingsContainer.AddChild(sideJobControl);
        RefreshNotes();
    }

    public void AddAcceptedSideJob(SideJobInfo sideJob)
    {
        var sideJobControl = new SideJobControl();
        sideJobControl.Update(sideJob);
        AcceptedJobListingsContainer.AddChild(sideJobControl);
        RefreshNotes();
    }

    private void RefreshNotes()
    {
        AcceptedJobListingsNote.Visible = AcceptedJobListingsContainer.ChildCount == 0;
        AvailableJobListingsNote.Visible = AvailableJobListingsContainer.ChildCount == 0;
    }
}
