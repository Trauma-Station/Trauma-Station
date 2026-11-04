// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;
using Content.Trauma.Shared.JobListings;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class SideJobControl : Control
{
    private IEntityManager _ent = default!;
    private SpriteSystem _sprite = default!;

    private NetEntity? _sideJob;

    public Action<NetEntity>? OnAccepted;
    public Action<NetEntity>? OnCancelled;
    public Action<NetEntity>? OnClaimed;

    public SideJobControl(IEntityManager ent, SpriteSystem sprite)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _ent = ent;
        _sprite = sprite;

        AcceptButton.OnPressed += _ =>
        {
            if (_sideJob is { } job)
                OnAccepted?.Invoke(job);
        };

        ClaimButton.OnPressed += _ =>
        {
            if (_sideJob is { } job)
                OnClaimed?.Invoke(job);
        };

        CancelButton.OnPressed += _ =>
        {
            if (_sideJob is { } job)
                OnCancelled?.Invoke(job);
        };
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
        ClaimButton.Visible = true;
        ProgressBar.Visible = true;
        ProgressLabel.Visible = true;
    }

    public void DisableAcceptButton()
    {
        AcceptButton.Disabled = false;
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
        _sideJob = _ent.GetNetEntity(info.Entity);
        if (_ent.TryGetComponent<SideJobComponent>(info.Entity, out var jobComp))
            CancelButton.Disabled = !jobComp.CanCancel;
    }
}
