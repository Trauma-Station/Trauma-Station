// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class LabelFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private LabelFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LabelFilterWindow>();
        _window.SetEntity(Owner);
        _window.OnSetLabel += label => SendPredictedMessage(new LabelFilterSetLabelMessage(label));
    }
}
