// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class NameFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private NameFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<NameFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnSetName += name => SendPredictedMessage(new NameFilterSetNameMessage(name));
        _window.OnSetMode += mode => SendPredictedMessage(new NameFilterSetModeMessage(mode));
    }
}
