// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class SideJobControl : Control
{
    [Dependency] private IEntityManager _entity = default!;
    private SpriteSystem _sprite;
    private NetEntity? _sideJob;

    public Action<NetEntity>? OnAccepted;

    public SideJobControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entity.System<SpriteSystem>();

        JobListingAcceptButton.OnPressed += _ =>
        {
            if (_sideJob is not null)
                OnAccepted?.Invoke(_sideJob.Value);
        };
    }

    public void Update(SideJobInfo info)
    {
        JobListingsName.Text = info.Title;
        JobListingDescription.Text = info.Description;
        JobListingTexture.Texture = _sprite.Frame0(info.Icon);
        JobListingReward.Text = Loc.GetString("job-listings-ui-reward", ("reward", info.RewardName));
        _sideJob = info.Entity;
    }
}
