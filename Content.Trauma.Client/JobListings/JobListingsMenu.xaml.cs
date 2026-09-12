// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class JobListingsMenu : FancyWindow
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityManager _entity = default!;
    private SpriteSystem _sprite = default!;
    private JobListingsSystem _jobs = default!;
    private Entity<JobListingsComponent> _jobBoard = default!;

    private TimeSpan? _refreshTimerBarTime;

    private int _reputation = 0;
    private List<SideJobInfo> _availableSideJobs = new();
    private List<SideJobInfo> _acceptedSideJobs = new();
    private bool _bonusRefresh;
    private TimeSpan? _refreshTime;

    public Action<NetEntity>? OnAccepted;
    public Action<NetEntity>? OnCancelled;
    public Action<NetEntity>? OnClaimed;
    public Action? OnRefresh;

    public JobListingsMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        RefreshButton.OnPressed += _ => OnRefresh?.Invoke();
        _sprite = _entity.System<SpriteSystem>();
        _jobs = _entity.System<JobListingsSystem>();
    }

    public void SetOwner(EntityUid owner)
    {
        if (_jobs.GetJobBoard(owner) is not { } jobBoard)
            return;

        _jobBoard = jobBoard;
        UpdateReputation();
        UpdateSideJobListings();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        Update();

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

    private void Update()
    {
        if (_reputation != _jobBoard.Comp.Reputation)
            UpdateReputation();

        if (!CompareSideJobLists(_availableSideJobs, _jobBoard.Comp.AvailableSideJobs) || !CompareSideJobLists(_acceptedSideJobs, _jobBoard.Comp.AcceptedSideJobs))
            UpdateSideJobListings();

        if (_bonusRefresh != _jobBoard.Comp.BonusRefresh || _refreshTime != _jobBoard.Comp.RefreshTime)
            UpdateRefreshProgressBar();
    }

    private void UpdateReputation()
    {
        _reputation = _jobBoard.Comp.Reputation;
        ReputationText.Text = Loc.GetString("job-listings-ui-reputation", ("reputation", _reputation));
        var title = Loc.GetString($"job-listings-ui-reputation-level-{_jobs.GetReputationLevel(_jobBoard)}");
        ReputationInfo.Text = Loc.GetString($"job-listings-ui-reputation-title", ("title", title));
    }

    private bool CompareSideJobLists(List<SideJobInfo> list, BaseContainer container)
    {
        foreach (var sideJob in list)
        {
            if (container.ContainedEntities.FirstOrNull(x => x == sideJob.Entity) is not { } match || _jobs.GetCachedProgress(match) != sideJob.Progress)
                return false;
        }
        return list.Count == container.Count;
    }

    private void UpdateSideJobListings()
    {
        ClearJobListings();
        _availableSideJobs = _jobs.GetAvailableSideJobsInfos(_jobBoard);
        _acceptedSideJobs = _jobs.GetAcceptedSideJobsInfos(_jobBoard);

        foreach (var sideJob in _availableSideJobs)
        {
            AddAvailableSideJob(sideJob);
        }
        foreach (var sideJob in _acceptedSideJobs)
        {
            AddAcceptedSideJob(sideJob);
        }

        RefreshListings(_jobBoard.Comp.MaximumAcceptedSideJobs);
    }

    private void ClearJobListings()
    {
        AvailableJobListingsContainer.RemoveAllChildren();
        AcceptedJobListingsContainer.RemoveAllChildren();
    }

    private SideJobControl CreateControl()
    {
        var control = new SideJobControl(_entity, _timing, _sprite);
        control.OnAccepted += job => OnAccepted?.Invoke(job);
        control.OnClaimed += job => OnClaimed?.Invoke(job);
        control.OnCancelled += job => OnCancelled?.Invoke(job);
        return control;
    }

    private void AddAvailableSideJob(SideJobInfo info)
    {
        var control = CreateControl();
        control.UpdateAsAvailable(info);
        AvailableJobListingsContainer.AddChild(control);
    }

    private void AddAcceptedSideJob(SideJobInfo info)
    {
        var control = CreateControl();
        control.UpdateAsAccepted(info);
        AcceptedJobListingsContainer.AddChild(control);
    }

    private void RefreshListings(int maximumAcceptedSideJobs)
    {
        AcceptedJobListingsNote.Visible = AcceptedJobListingsContainer.ChildCount == 0;
        AvailableJobListingsNote.Visible = AvailableJobListingsContainer.ChildCount == 0;

        AvailableJobListingsNote.Text = Loc.GetString($"job-listings-ui-no-available-note");

        if (AcceptedJobListingsContainer.ChildCount >= maximumAcceptedSideJobs)
            DisableAcceptButtons();
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

    private void UpdateRefreshProgressBar()
    {
        _bonusRefresh = _jobBoard.Comp.BonusRefresh;
        _refreshTime = _jobBoard.Comp.RefreshTime;

        RefreshButton.Disabled = true;

        if (_bonusRefresh)
        {
            RefreshTimerProgressBar.MaxValue = 1;
            RefreshTimerProgressBar.Value = 1;
            RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label-available");
            RefreshButton.Disabled = false;
            _refreshTimerBarTime = null;
            return;
        }

        if (_refreshTime is null)
        {
            RefreshTimerProgressBar.MaxValue = 1;
            RefreshTimerProgressBar.Value = 0;
            RefreshTimerLabel.Text = Loc.GetString("job-listings-ui-refresh-timer-label", ("time", FormatTime(_jobBoard.Comp.RefreshWaitDuration)));
            _refreshTimerBarTime = null;
            return;
        }

        RefreshTimerProgressBar.MaxValue = (float) _jobBoard.Comp.RefreshWaitDuration.TotalSeconds;
        _refreshTimerBarTime = _refreshTime.Value;
    }

    private string FormatTime(TimeSpan time)
    {
        return $"{Math.Floor(time.TotalMinutes):0}m {time.Seconds}s";
    }
}
