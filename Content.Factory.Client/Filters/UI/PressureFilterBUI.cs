// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class PressureFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private PressureFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PressureFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnSetMin += min => SendPredictedMessage(new PressureFilterSetMinMessage(min));
        _window.OnSetMax += max => SendPredictedMessage(new PressureFilterSetMaxMessage(max));
    }
}
