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
    public Action<NetEntity>? OnJobClaimed;

    public int MaximumAcceptedSideJobs;

    public JobListingsMenu()
    {
        RobustXamlLoader.Load(this);
    }

    public void ClearJobListings()
    {
        AvailableJobListingsContainer.RemoveAllChildren();
        AcceptedJobListingsContainer.RemoveAllChildren();
    }

    public void AddAvailableSideJob(SideJobInfo info)
    {
        var control = CreateControl(info);
        control.UpdateAsAvailable(info);
        AvailableJobListingsContainer.AddChild(control);
        Refresh();
    }

    public void AddAcceptedSideJob(SideJobInfo info)
    {
        var control = CreateControl(info);
        control.UpdateAsAccepted(info);
        AcceptedJobListingsContainer.AddChild(control);
        Refresh();
    }

    public void SetReputation(int reputation)
    {
        ReputationText.Text = Loc.GetString("job-listings-ui-reputation", ("reputation", reputation));
    }

    private SideJobControl CreateControl(SideJobInfo info)
    {
        var control = new SideJobControl();
        control.OnAccepted += job => OnAccepted(job, control, info);
        control.OnClaimed += job => OnClaimed(job, control, info);
        control.OnCancelled += job => OnCancelled(job, control, info);
        return control;
    }

    private void DisableAcceptButtons()
    {
        foreach (var control in AvailableJobListingsContainer.Children)
        {
            if (control is not SideJobControl sideJobControl)
                continue;
            sideJobControl.AcceptButton.Disabled = true;
        }
    }

    private void EnableAcceptButtons()
    {
        foreach (var control in AvailableJobListingsContainer.Children)
        {
            if (control is not SideJobControl sideJobControl)
                continue;
            sideJobControl.AcceptButton.Disabled = false;
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

    private void OnClaimed(NetEntity job, SideJobControl control, SideJobInfo info)
    {
        // predict ui change
        AcceptedJobListingsContainer.RemoveChild(control);
        Refresh();
        // invoke event
        OnJobClaimed?.Invoke(job);
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
        if (AcceptedJobListingsContainer.ChildCount >= MaximumAcceptedSideJobs)
            DisableAcceptButtons();
        else
            EnableAcceptButtons();
    }
}
