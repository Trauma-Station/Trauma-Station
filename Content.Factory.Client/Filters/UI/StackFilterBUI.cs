// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class StackFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private StackFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StackFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnSetMin += min => SendPredictedMessage(new StackFilterSetMinMessage(min));
        _window.OnSetSize += size => SendPredictedMessage(new StackFilterSetSizeMessage(size));
    }
}
