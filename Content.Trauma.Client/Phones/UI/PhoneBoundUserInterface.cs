// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Phones.Components;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Phones.UI;

public sealed class PhoneBoundUserInterface : BoundUserInterface
{
    private PhoneMenu? _menu;

    public PhoneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<PhoneMenu>();

        _menu.OnKeypadButtonPressed += i =>
        {
            var current = _menu.DialNumber.GetMessage();

            if (current is not {})
                return;

            if (current.Length >= 5)
                return;

            _menu.DialNumber.SetMessage(current + i);

            SendPredictedMessage(new PhoneKeypadMessage(i));
        };
        _menu.OnEnterButtonPressed += () => SendPredictedMessage(new PhoneDialedMessage());
        _menu.OnClearButtonPressed += () =>
        {
            SendPredictedMessage(new PhoneKeypadClearMessage());
            _menu.DialNumber.SetMessage(string.Empty);
        };

        _menu.OnPhoneBookButtonPressed += i =>
        {
            SendPredictedMessage(new PhoneBookPressedMessage(i));
            _menu.DialNumber.SetMessage(i.ToString());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_menu == null || state is not GoobPhoneBuiState s)
            return;

        Refresh(s);
    }

    private void Refresh(GoobPhoneBuiState state)
    {
        if (_menu == null)
            return;

        _menu.ClearPhoneBook();

        foreach (var phone in state.Phones)
        {
            _menu.AddPhoneBookLabel(
                phone.Name,
                phone.Category,
                phone.Number
            );
        }
    }
}
