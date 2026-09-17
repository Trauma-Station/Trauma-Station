// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.HoloParasite;

namespace Content.Trauma.Client.HoloParasite.UI;

public sealed partial class HoloParasitePickerBoundUserInterface : BoundUserInterface
{
    private HoloParasitePickerWindow? _panel;
    private List<HoloParasiteChoice> _choices = new();
    private int _currentChoice;

    public HoloParasitePickerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _panel = this.CreateWindow<HoloParasitePickerWindow>();
        _panel.OnVariantPicked += index =>
        {
            _currentChoice = index;
            _panel?.RenderChoice(_choices[index]);
        };
        _panel.OnBindPressed += () =>
        {
            if (_currentChoice < 0 || _currentChoice >= _choices.Count)
                return;

            SendMessage(new HoloParasitePickMessage(_choices[_currentChoice].ProtoId));
            _panel?.Close();
        };
        _panel.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not HoloParasitePickerState pickerState)
            return;

        _choices = pickerState.Choices;
        _currentChoice = Math.Clamp(pickerState.StartIndex, 0, Math.Max(0, _choices.Count - 1));

        _panel?.PopulateChoices(_choices, _currentChoice);
        if (_choices.Count > 0)
            _panel?.RenderChoice(_choices[_currentChoice]);
    }
}