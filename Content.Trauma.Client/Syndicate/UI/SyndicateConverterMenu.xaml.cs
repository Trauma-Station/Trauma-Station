// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Materials;
using Content.Client.Message;
using Content.Client.UserInterface.Controls;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Content.Trauma.Shared.Syndicate.Components;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Client.Syndicate.UI;

[GenerateTypedNameReferences]
public sealed partial class SyndicateConverterMenu : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private readonly ItemSlotsSystem _itemSlots;
    private readonly SyndicateConverterSystem _syndicateConverter;
    private readonly MaterialStorageSystem _materialStorage;

    private EntityUid _owner;

    public static readonly EntProtoId NoItemEffectId = "FlatpackerNoBoardEffect";

    private EntityUid? _currentItem;

    public event Action? ConvertButtonPressed;

    public SyndicateConverterMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _itemSlots = _ent.System<ItemSlotsSystem>();
        _syndicateConverter = _ent.System<SyndicateConverterSystem>();
        _materialStorage = _ent.System<MaterialStorageSystem>();

        ConvertButton.OnPressed += _ => ConvertButtonPressed?.Invoke();

        InsertLabel.SetMarkup(Loc.GetString("syndicate-flatpacker-ui-insert-item"));
    }

    public void SetEntity(EntityUid uid)
    {
        _owner = uid;
        MaterialStorageControl.SetOwner(uid);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_ent.TryGetComponent<SyndicateConverterComponent>(_owner, out var converter) ||
            !_itemSlots.TryGetSlot(_owner, converter.SlotId, out var itemSlot))
            return;

        var converterEntity = new Entity<SyndicateConverterComponent>(_owner, converter);

        Entity<SyndicateConvertibleComponent>? currentItemEntity = default;
        SyndicateConvertibleComponent? currentItemComp = null;

        if (_currentItem != null)
        {
            if (_ent.TryGetComponent<SyndicateConvertibleComponent>(_currentItem, out currentItemComp))
                currentItemEntity = new Entity<SyndicateConvertibleComponent>((EntityUid) _currentItem, currentItemComp);
            else
                currentItemComp = null;
        }
        else
            currentItemEntity = null;

        if (converter.Converting)
            ConvertButton.Disabled = true;
        else if (currentItemEntity != null)
        {
            ConvertButton.Disabled =
            !_syndicateConverter.TryGetConversionCost(converterEntity, (Entity<SyndicateConvertibleComponent>) currentItemEntity, out var curCost) ||
            !_materialStorage.CanChangeMaterialAmount(_owner, curCost);
        }

        if (_currentItem == itemSlot.Item)
            return;

        _currentItem = itemSlot.Item;
        if (_currentItem != null && _ent.TryGetComponent<SyndicateConvertibleComponent>(_currentItem, out currentItemComp))
            currentItemEntity = new Entity<SyndicateConvertibleComponent>((EntityUid) _currentItem, currentItemComp);
        CostHeaderLabel.Visible = false;
        InsertLabel.Visible = _currentItem == null;

        if (currentItemEntity is null)
        {
            ItemSprite.SetPrototype(NoItemEffectId);
            CostLabel.SetMessage(Loc.GetString("syndicate-flatpacker-ui-no-item-label"));
            ItemNameLabel.SetMessage(string.Empty);
            ConvertButton.Disabled = true;
            return;
        }
        else if (_syndicateConverter.TryGetConvertedPrototype((Entity<SyndicateConvertibleComponent>) currentItemEntity, out var prototype)
            && _syndicateConverter.TryGetConversionCost(converterEntity, (Entity<SyndicateConvertibleComponent>) currentItemEntity, out var cost))
        {
            var proto = _proto.Index<EntityPrototype>(prototype);
            ItemSprite.SetPrototype(prototype);
            ItemNameLabel.SetMessage(proto.Name);
            CostLabel.SetMarkup(GetCostString(cost));
            CostHeaderLabel.Visible = true;
        }
        else
        {
            ItemSprite.SetPrototype(NoItemEffectId);
            CostLabel.SetMarkup(Loc.GetString("syndicate-flatpacker-ui-item-invalid-label"));
            ItemNameLabel.SetMessage(string.Empty);
            ConvertButton.Disabled = true;
        }

    }

    private string GetCostString(Dictionary<string, int> costs)
    {
        var orderedCosts = costs.OrderBy(p => p.Value).ToArray();
        var msg = new FormattedMessage();
        for (var i = 0; i < orderedCosts.Length; i++)
        {
            var (mat, amount) = orderedCosts[i];

            var matProto = _proto.Index<MaterialPrototype>(mat);

            var sheetVolume = _materialStorage.GetSheetVolume(matProto);
            var sheets = (float) -amount / sheetVolume;
            var amountText = Loc.GetString("lathe-menu-material-amount",
                ("amount", sheets),
                ("unit", Loc.GetString(matProto.Unit)));
            var text = Loc.GetString("lathe-menu-tooltip-display",
                ("amount", amountText),
                ("material", Loc.GetString(matProto.Name)));

            msg.TryAddMarkup(text, out _);

            if (i != orderedCosts.Length - 1)
                msg.PushNewline();
        }

        return msg.ToMarkup();
    }
}
