// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Phones.Components;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Phones.UI;

public sealed class PhoneNameChangeUI : BoundUserInterface
{
    private ChangePhoneName? _menu;

    public PhoneNameChangeUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChangePhoneName>();

        _menu.OnTextChanged += i =>
        {
            SendPredictedMessage(new PhoneNameChangedMessage(i));
        };
        _menu.OnCategoryChanged += i =>
        {
            SendPredictedMessage(new PhoneCategoryChangedMessage(i));
        };
    }
}
