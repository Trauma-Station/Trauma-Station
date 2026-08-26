// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : DefaultWindow
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IGameTiming _timing = default!;
    private SharedJobListingsSystem _jobs = default!;

    private TimeSpan? _refreshTimerBarTime;

    public Action<NetEntity>? OnAccepted;
    public Action<NetEntity>? OnCancelled;
    public Action<NetEntity>? OnClaimed;
    public Action? OnRefresh;

    public JobListingsMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        RefreshButton.OnPressed += _ => OnRefresh?.Invoke();
        _jobs = _entity.System<SharedJobListingsSystem>();
    }

    public void Update(EntityUid owner)
    {
        if (!_jobs.GetJobBoard(owner, out var jobBoard))
            return;
        var availableSideJobInfos = _jobs.GetAvailableSideJobsInfo(jobBoard.Value);
        var acceptedSideJobInfos = _jobs.GetAcceptedSideJobsInfo(jobBoard.Value);

        ClearJobListings();
        foreach (var sideJob in availableSideJobInfos)
        {
            AddAvailableSideJob(sideJob);
        }
        foreach (var sideJob in acceptedSideJobInfos)
        {
            AddAcceptedSideJob(sideJob);
        }

        var reputationLevel = _jobs.GetReputationLevel(jobBoard.Value);
        SetReputation(jobBoard.Value.Comp.Reputation, reputationLevel);
        SetRefresh(jobBoard.Value.Comp.BonusRefresh, jobBoard.Value.Comp.RefreshTime, jobBoard.Value.Comp.RefreshWaitDuration);
        Refresh(jobBoard.Value.Comp.MaximumAcceptedSideJobs);
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
    }

    public void AddAcceptedSideJob(SideJobInfo info)
    {
        var control = CreateControl(info);
        control.UpdateAsAccepted(info);
        AcceptedJobListingsContainer.AddChild(control);
    }

    public void SetReputation(int reputation, int level)
    {
        ReputationText.Text = Loc.GetString("job-listings-ui-reputation", ("reputation", reputation));
        var title = Loc.GetString($"job-listings-ui-reputation-level-{level}");
        ReputationInfo.Text = Loc.GetString($"job-listings-ui-reputation-title", ("title", title));
    }

    public void SetRefresh(bool bonus, TimeSpan? refreshTime, TimeSpan refreshWaitDuration)
    {
        RefreshButton.Disabled = true;

        if (bonus)
        {
            RefreshTimerProgressBar.MaxValue = 1;
            RefreshTimerProgressBar.Value = 1;
            RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label-available");
            RefreshButton.Disabled = false;
            _refreshTimerBarTime = null;
            return;
        }

        if (refreshTime is null)
        {
            RefreshTimerProgressBar.MaxValue = 1;
            RefreshTimerProgressBar.Value = 0;
            RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label", ("time", FormatTime(refreshWaitDuration)));
            _refreshTimerBarTime = null;
            return;
        }

        RefreshTimerProgressBar.MaxValue = (float) refreshWaitDuration.TotalSeconds;
        _refreshTimerBarTime = refreshTime.Value;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_refreshTimerBarTime is null)
            return;

        RefreshButton.Disabled = true;

        if (_timing.CurTime >= _refreshTimerBarTime.Value)
        {
            RefreshTimerProgressBar.MaxValue = 1;
            RefreshTimerProgressBar.Value = 1;
            RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label-available");
            RefreshButton.Disabled = false;
            return;
        }

        var time = _refreshTimerBarTime.Value - _timing.CurTime;
        RefreshTimerProgressBar.Value = RefreshTimerProgressBar.MaxValue - (float) time.TotalSeconds;
        RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label", ("time", FormatTime(time)));
    }

    private String FormatTime(TimeSpan time)
    {
        return $"{Math.Floor(time.TotalMinutes):0}m {time.Seconds}s";
    }

    private SideJobControl CreateControl(SideJobInfo info)
    {
        var control = new SideJobControl();
        control.OnAccepted += job => OnAccepted?.Invoke(job);
        control.OnClaimed += job => OnClaimed?.Invoke(job);
        control.OnCancelled += job => OnCancelled?.Invoke(job);
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

    private void Refresh(int maximumAcceptedSideJobs)
    {
        AcceptedJobListingsNote.Visible = AcceptedJobListingsContainer.ChildCount == 0;
        AvailableJobListingsNote.Visible = AvailableJobListingsContainer.ChildCount == 0;
        if (AcceptedJobListingsContainer.ChildCount >= maximumAcceptedSideJobs)
            DisableAcceptButtons();
        else
            EnableAcceptButtons();
    }
}
