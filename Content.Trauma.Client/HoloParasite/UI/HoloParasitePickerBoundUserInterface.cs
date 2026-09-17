// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.HoloParasite;

namespace Content.Trauma.Client.HoloParasite.UI;

public sealed partial class HoloParasitePickerBoundUserInterface : BoundUserInterface
{
    private HoloParasitePickerWindow? _panel;
    private List<HoloParasiteVariant> _variants = new();
    private int _currentChoice;

    public HoloParasitePickerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _variants = EntMan.GetComponent<HoloParasitePickerComponent>(Owner).Variants;
        _currentChoice = 0;

        _panel = this.CreateWindow<HoloParasitePickerWindow>();
        _panel.OnVariantPicked += index =>
        {
            _currentChoice = index;
            _panel?.RenderChoice(index);
        };
        _panel.OnBindPressed += () =>
        {
            if (_currentChoice < 0 || _currentChoice >= _variants.Count)
                return;

            SendMessage(new HoloParasitePickMessage(_variants[_currentChoice].Prototype.Id));
            _panel?.Close();
        };
        _panel.PopulateChoices(_variants);
        _panel.OpenCentered();
    }
}