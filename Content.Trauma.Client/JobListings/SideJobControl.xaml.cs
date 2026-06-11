// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.JobListings;

namespace Content.Trauma.Client.JobListings;

[GenerateTypedNameReferences]
public sealed partial class SideJobControl : Control
{
    [Dependency] private IEntityManager _entity = default!;
    private SpriteSystem _sprite;

    public SideJobControl()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        _sprite = _entity.System<SpriteSystem>();
    }

    public void Update(SideJobInfo info)
    {
        JobListingsName.Text = info.Title;
        JobListingDescription.Text = info.Description;
        JobListingTexture.Texture = _sprite.Frame0(info.Icon);
        JobListingReward.Text = Loc.GetString("job-listings-ui-reward", ("reward", info.RewardName));
    }
}
