// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.HoloParasite;

namespace Content.Trauma.Client.HoloParasite.UI;

[GenerateTypedNameReferences]
public sealed partial class HoloParasitePickerWindow : FancyWindow
{
    public event Action<int>? OnVariantPicked;
    public event Action? OnBindPressed;

    private readonly List<Button> _cards = new();
    private readonly ButtonGroup _cardGroup = new();
    private List<HoloParasiteVariant> _variants = new();
    private int _pickedIndex = -1;

    public HoloParasitePickerWindow()
    {
        RobustXamlLoader.Load(this);
        BindButton.OnPressed += _ => OnBindPressed?.Invoke();
    }

    public void PopulateChoices(List<HoloParasiteVariant> variants)
    {
        _variants = variants;
        _cards.Clear();
        VariantGrid.RemoveAllChildren();

        for (var i = 0; i < variants.Count; i++)
        {
            var index = i;
            var variant = variants[i];

            var card = new Button
            {
                Text = variant.Caption,
                HorizontalExpand = true,
                VerticalExpand = true,
                MinSize = new Vector2(0, 36),
                ClipText = true,
                ToggleMode = true,
                Group = _cardGroup,
            };

            card.OnPressed += _ =>
            {
                _pickedIndex = index;
                OnVariantPicked?.Invoke(index);
            };

            _cards.Add(card);
            VariantGrid.AddChild(card);
        }

        if (variants.Count > 0)
            RenderChoice(0);
        else
            BindButton.Disabled = true;
    }

    public void RenderChoice(int index)
    {
        if (index < 0 || index >= _variants.Count)
            return;

        _pickedIndex = index;
        for (var i = 0; i < _cards.Count; i++)
            _cards[i].Pressed = i == index;

        var variant = _variants[index];
        Preview.SetPrototype(variant.Prototype.Id);
        VariantCaption.Text = variant.Caption;
        VariantSynopsis.SetMessage(variant.Synopsis ?? string.Empty);
        VariantLore.SetMessage(variant.Lore ?? string.Empty);
        BindButton.Disabled = false;
    }
}