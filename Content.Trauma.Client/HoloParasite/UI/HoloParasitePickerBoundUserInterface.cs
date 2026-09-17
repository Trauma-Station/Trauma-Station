// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.HoloParasite;

namespace Content.Trauma.Client.HoloParasite.UI;

public sealed partial class HoloParasitePickerBoundUserInterface : BoundUserInterface
{
    private HoloParasitePickerWindow? _panel;

    public HoloParasitePickerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var variants = EntMan.GetComponent<HoloParasitePickerComponent>(Owner).Variants;
        var currentChoice = 0;

        _panel = this.CreateWindow<HoloParasitePickerWindow>();
        _panel.OnVariantPicked += index =>
        {
            currentChoice = index;
            _panel?.RenderChoice(index);
        };
        _panel.OnBindPressed += () =>
        {
            if (currentChoice < 0 || currentChoice >= variants.Count)
                return;

            SendMessage(new HoloParasitePickMessage(variants[currentChoice].Prototype.Id));
            _panel?.Close();
        };
        _panel.PopulateChoices(variants);
        _panel.OpenCentered();
    }
}