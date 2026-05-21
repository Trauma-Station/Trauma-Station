// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Forging;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Trauma.Client.Forging;

public sealed partial class ForgingAnvilBUI : BoundUserInterface
{
    private readonly ForgingSystem _forging;
    private readonly SharedMetalSystem _metal;
    private SimpleRadialMenu _metals;
    private SimpleRadialMenu _items;
    private MetalPrototype _chosenMetal = default!;
    private Color _color = Color.White;

    public static readonly ResPath IngotRsi = new("/Textures/_Trauma/Objects/Specific/forging.rsi");

    public ForgingAnvilBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _metals = this.CreateDisposableControl<SimpleRadialMenu>();
        _metals.OnClose += () =>
        {
            if (_chosenMetal == default!)
                Close(); // only close if you abort rather than start picking an item
        };
        _items = this.CreateWindow<SimpleRadialMenu>();
        _items.Close();
        _forging = EntMan.System<ForgingSystem>();
        _metal = EntMan.System<SharedMetalSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _metals.SetButtons(GetMetalsButtons());
        _metals.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> GetMetalsButtons()
    {
        var buttons = new List<RadialMenuOptionBase>(_metal.AllMetals.Count);
        foreach (var metal in _metal.AllMetals)
        {
            var icon = new SpriteSpecifier.Rsi(IngotRsi, metal.IngotSprite);
            buttons.Add(new RadialMenuActionOption<MetalPrototype>(OnMetalSelected, metal)
            {
                ToolTip = metal.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(icon)
            });
        }
        return buttons;
    }

    private List<RadialMenuOptionBase> GetItemsButtons()
    {
        var buttons = new List<RadialMenuOptionBase>(_forging.AllItems.Count);
        foreach (var (category, items) in _forging.AllItems)
        {
            var nested = GetItemsButtons(items);
            if (nested.Count == 0)
                continue; // don't add a category if none of its items can be made

            buttons.Add(new RadialMenuNestedLayerOption(nested)
            {
                ToolTip = category.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(category.Icon),
                Color = _color
            });
        }
        return buttons;
    }

    private List<RadialMenuOptionBase> GetItemsButtons(List<ForgedItemPrototype> items)
    {
        var buttons = new List<RadialMenuOptionBase>(items.Count);
        foreach (var item in items)
        {
            if (!_forging.CanMakeFrom(item, _chosenMetal))
                continue; // dont show illegal recipes, server wont allow it anyway

            buttons.Add(new RadialMenuActionOption<ForgedItemPrototype>(OnItemSelected, item)
            {
                ToolTip = item.Name,
                IconSpecifier = item.Result is {} result
                    ? RadialMenuIconSpecifier.With(result)
                    : RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(item.Sprite!.Value, "icon")),
                Color = _color
            });
        }
        return buttons;
    }

    private void OnMetalSelected(MetalPrototype metal)
    {
        _chosenMetal = metal;
        _color = metal.Color;
        _metals.Close();
        _items.SetButtons(GetItemsButtons());
        _items.OpenOverMouseScreenPosition();
    }

    private void OnItemSelected(ForgedItemPrototype item)
    {
        SendPredictedMessage(new AnvilStartItemMessage(_chosenMetal.ID, item.ID));
        Close();
    }
}
