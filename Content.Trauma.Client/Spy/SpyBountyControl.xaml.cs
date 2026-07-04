using Content.Shared.Store;
using Content.Trauma.Shared.Spy;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyBountyControl : Control
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IEntityManager _entity = default!;

    public SpyBountyControl(SpyBounty data)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        var spriteSys = _entity.System<SpriteSystem>();

        BountyName.Text = data.Name;
        BountyName.SetOnlyStyleClass($"SpyBounty{data.Difficulty}");

        BountyDescription.Text = Loc.GetString("spy-uplink-description-label", ("desc", data.Description));
        if (data.Sprite is { } sprite)
            BountyTexture.Texture = spriteSys.Frame0(sprite);
        else if (data.Protos is { } protos)
            BountyTexture.Texture = spriteSys.Frame0(_prototype.Index(protos[0]));

        var listing = _prototype.Index(data.Reward);

        BountyReward.Title = Loc.GetString("spy-uplink-reward",
            ("reward",
                ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _prototype)));
        BountyRewardDescription.Text = Loc.GetString("spy-uplink-description-label",
            ("desc",
                ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(listing, _prototype)));

        Texture? texture = null;

        if (listing.Icon is { } icon)
            texture = spriteSys.Frame0(icon);

        if (listing.ProductEntity is { } ent)
            texture ??= spriteSys.GetPrototypeIcon(ent).Default;

        BountyRewardTexture.Texture = texture;

        ClaimedPanel.Visible = data.Claimed;
    }
}
