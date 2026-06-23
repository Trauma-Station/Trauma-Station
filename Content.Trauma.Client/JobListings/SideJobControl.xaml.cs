// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class SideJobControl : Control
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IGameTiming _timing = default!;
    private SpriteSystem _sprite;

    private NetEntity? _sideJob;
    private bool _cancelAlreadyPressed = false;
    private TimeSpan? _cancelPressTime;

    private readonly TimeSpan _cancelSafetyDuration = TimeSpan.FromSeconds(1.0);

    public Action<NetEntity>? OnAccepted;
    public Action<NetEntity>? OnCancelled;
    public Action<NetEntity>? OnClaimed;

    public SideJobControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entity.System<SpriteSystem>();
        AcceptButton.OnPressed += OnAcceptButtonPressed;
        CancelButton.OnPressed += OnCancelButtonPressed;
        ClaimButton.OnPressed += OnClaimButtonPressed;
    }

    public void UpdateAsAvailable(SideJobInfo info)
    {
        Update(info);
        AcceptButton.Visible = true;
        CancelButton.Visible = false;
        ClaimButton.Visible = false;
        ProgressBar.Visible = false;
        ProgressLabel.Visible = false;
    }

    public void UpdateAsAccepted(SideJobInfo info)
    {
        Update(info);
        AcceptButton.Visible = false;
        CancelButton.Visible = true;
        CancelButton.Text = Loc.GetString("job-listings-ui-cancel-button");
        _cancelAlreadyPressed = false;
        ClaimButton.Visible = true;
        ProgressBar.Visible = true;
        ProgressLabel.Visible = true;
    }

    private void OnAcceptButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (_sideJob is not null)
            OnAccepted?.Invoke(_sideJob.Value);
    }

    private void OnClaimButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (_sideJob is not null)
            OnClaimed?.Invoke(_sideJob.Value);
    }

    private void OnCancelButtonPressed(BaseButton.ButtonEventArgs args)
    {
        if (_sideJob is null)
            return;

        if (!_cancelAlreadyPressed)
        {
            _cancelAlreadyPressed = true;
            _cancelPressTime = _timing.CurTime;
            CancelButton.Text = Loc.GetString("job-listings-ui-confirmation-button");
            return;
        }

        OnCancelled?.Invoke(_sideJob.Value);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_cancelAlreadyPressed && _cancelPressTime is not null && _timing.CurTime >= _cancelPressTime.Value + _cancelSafetyDuration)
        {
            _cancelAlreadyPressed = false;
            CancelButton.Text = Loc.GetString("job-listings-ui-cancel-button");
        }
    }

    private void Update(SideJobInfo info)
    {
        NameLabel.Text = info.Title;
        DescriptionLabel.Text = info.Description;
        ListingTexture.Texture = _sprite.Frame0(info.Icon);
        RewardLabel.Text = Loc.GetString("job-listings-ui-reward", ("reward", info.RewardName), ("rep", info.ReputationGain));
        var canClaim = info.Progress >= 0.999f;
        ClaimButton.Disabled = !canClaim;
        ProgressBar.Value = info.Progress;
        ProgressLabel.Text = Loc.GetString("job-listings-ui-progress", ("progress", info.Progress.ToString("P0")));
        _sideJob = info.Entity;
    }
}
