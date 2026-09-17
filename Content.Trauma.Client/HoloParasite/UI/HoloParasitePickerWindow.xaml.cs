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
    private List<HoloParasiteChoice> _choices = new();
    private int _pickedIndex = -1;

    public HoloParasitePickerWindow()
    {
        RobustXamlLoader.Load(this);
        BindButton.OnPressed += _ => OnBindPressed?.Invoke();
    }

    public void PopulateChoices(List<HoloParasiteChoice> choices, int defaultIndex)
    {
        _choices = choices;
        _cards.Clear();
        VariantGrid.RemoveAllChildren();

        for (var i = 0; i < choices.Count; i++)
        {
            var index = i;
            var choice = choices[i];

            var card = new Button
            {
                Text = choice.Caption,
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

        if (choices.Count > 0)
            RenderChoice(choices[Math.Clamp(defaultIndex, 0, choices.Count - 1)]);
        else
            BindButton.Disabled = true;
    }

    public void RenderChoice(HoloParasiteChoice choice)
    {
        var index = _choices.IndexOf(choice);
        if (index >= 0)
        {
            _pickedIndex = index;
            for (var i = 0; i < _cards.Count; i++)
                _cards[i].Pressed = i == index;
        }

        Preview.SetPrototype(choice.ProtoId);
        VariantCaption.Text = choice.Caption;
        VariantSynopsis.SetMessage(choice.Synopsis ?? string.Empty);
        VariantLore.SetMessage(choice.Lore ?? string.Empty);
        BindButton.Disabled = false;
    }
}