using Content.Shared.Store;
using Content.Trauma.Shared.Spy;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyBountyControl : Control
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _entity = default!;

    public SpyBountyControl(SpyBounty data)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        var spriteSys = _entity.System<SpriteSystem>();

        var bountyProto = _proto.Index(data.BountyProto);
        var rewardProto = _proto.Index(data.Reward);
        var listing = _proto.Index(rewardProto.RewardSelection[0]);

        BountyName.Text = data.Name;
        BountyName.SetOnlyStyleClass($"SpyBounty{bountyProto.Difficulty}");

        BountyDescription.Text = Loc.GetString("spy-uplink-description-label", ("desc", data.Description));
        if (data.Sprite is { } sprite)
            BountyTexture.Texture = spriteSys.Frame0(sprite);
        else if (data.Protos is { } protos)
            BountyTexture.Texture = spriteSys.Frame0(_proto.Index(protos[0]));

        BountyReward.Title = Loc.GetString("spy-uplink-reward",
            ("reward",
                rewardProto.RewardNameOverride is { } overrideName
                    ? Loc.GetString(overrideName)
                    : ListingLocalisationHelpers.GetLocalisedNameOrEntityName(listing, _proto)));
        BountyRewardDescription.Text = Loc.GetString("spy-uplink-description-label",
            ("desc",
                rewardProto.RewardDescriptionOverride is { } overrideDesc
                    ? Loc.GetString(overrideDesc)
                    : ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(listing, _proto)));

        Texture? texture = null;

        if (listing.Icon is { } icon)
            texture = spriteSys.Frame0(icon);

        if (listing.ProductEntity is { } ent)
            texture ??= spriteSys.GetPrototypeIcon(ent).Default;

        BountyRewardTexture.Texture = texture;

        ClaimedPanel.Visible = data.Claimed;
    }
}
