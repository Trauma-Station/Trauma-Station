// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Plumbing;

namespace Content.Factory.Client.Plumbing.UI;

public sealed class PlumbingFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private PlumbingFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PlumbingFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnChange += id => SendPredictedMessage(new PlumbingFilterChangeMessage(id));
    }
}
