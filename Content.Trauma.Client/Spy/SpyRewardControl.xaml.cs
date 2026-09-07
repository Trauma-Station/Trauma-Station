// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.Store;
using Content.Trauma.Shared.Spy;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyRewardControl : Control
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IEntityManager _ent = default!;

    private readonly SpriteSystem _sprite = default!;
    private ProtoId<ListingPrototype>? _selected;

    public event Action<SpyRewardControl, string, ProtoId<ListingPrototype>>? OnCollect;

    public SpyRewardControl(string id)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _sprite = _ent.System<SpriteSystem>();

        CollectButton.OnPressed += _ =>
        {
            if (_selected is { } selected)
                OnCollect?.Invoke(this, id, selected);
        };

        RewardsList.ItemPressed += (args, data) =>
        {
            SetListing(((ListingListData) data).Proto);
        };

        RewardsList.GenerateItem += GenerateButton;

        if (_proto.HasIndex<SpyRewardPrototype>(id))
            InitializeRewardProto(_proto.Index<SpyRewardPrototype>(id));
        else
            SetListing(id);
    }

    private void GenerateButton(ListData data, ListContainerButton button)
    {
        if (data is not ListingListData cast)
            return;

        var proto = _proto.Index(cast.Proto);

        var label = new Label
        {
            Text = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(proto, _proto),
            Margin = new Thickness(2),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
        };

        button.AddChild(label);
    }

    public void SetListing(ProtoId<ListingPrototype> protoId)
    {
        _selected = protoId;
        var proto = _proto.Index(protoId);

        RewardName.Text = ListingLocalisationHelpers.GetLocalisedNameOrEntityName(proto, _proto);
        RewardDescription.Text = ListingLocalisationHelpers.GetLocalisedDescriptionOrEntityDescription(proto, _proto);

        Texture? texture = null;

        if (proto.Icon is { } icon)
            texture = _sprite.Frame0(icon);

        if (proto.ProductEntity is { } ent)
            texture ??= _sprite.GetPrototypeIcon(ent).Default;

        RewardTexture.Texture = texture;
    }

    public void PopulateListings(List<ProtoId<ListingPrototype>> listings)
    {
        RewardSelection.Visible = listings.Count > 1;

        List<ListingListData> listData = new();
        foreach (var listing in listings)
        {
            if (_selected == null)
                SetListing(listing);

            listData.Add(new(listing));
        }

        RewardsList.PopulateList(listData);
        RewardsList.Select(RewardsList.Data[0]);
    }

    private void InitializeRewardProto(SpyRewardPrototype proto)
    {
        if (proto.RewardSelection.Count == 0)
            return;

        PopulateListings(proto.RewardSelection);
    }
}

public record ListingListData(ProtoId<ListingPrototype> Proto) : ListData;
