// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Common.JobListings;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : DefaultWindow
{
    public Action<NetEntity>? OnJobAccepted;
    public Action<NetEntity>? OnJobCancelled;

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
        sideJobControl.OnAccepted += job => OnAccepted(job, sideJobControl, sideJob);
        AvailableJobListingsContainer.AddChild(sideJobControl);
        Refresh();
    }

    public void AddAcceptedSideJob(SideJobInfo sideJob)
    {
        var sideJobControl = new SideJobControl();
        sideJobControl.UpdateAsAccepted(sideJob);
        sideJobControl.OnCancelled += job => OnCancelled(job, sideJobControl, sideJob);
        AcceptedJobListingsContainer.AddChild(sideJobControl);
        Refresh();
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

    private void OnAccepted(NetEntity job, SideJobControl control, SideJobInfo info)
    {
        // predict the ui change
        AvailableJobListingsContainer.RemoveChild(control);
        control.UpdateAsAccepted(info);
        AcceptedJobListingsContainer.AddChild(control);
        Refresh();
        // invoke the event so the BUI sends a message to the server and calculates the REAL state
        OnJobAccepted?.Invoke(job);
    }

    private void OnCancelled(NetEntity job, SideJobControl control, SideJobInfo info)
    {
        // predict ui change
        AcceptedJobListingsContainer.RemoveChild(control);
        Refresh();
        // invoke event
        OnJobCancelled?.Invoke(job);
    }

    private void Refresh()
    {
        AcceptedJobListingsNote.Visible = AcceptedJobListingsContainer.ChildCount == 0;
        AvailableJobListingsNote.Visible = AvailableJobListingsContainer.ChildCount == 0;
    }
}
