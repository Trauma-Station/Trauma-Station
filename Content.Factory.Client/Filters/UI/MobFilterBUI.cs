// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class MobFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private MobFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MobFilterWindow>();
        if (EntMan.TryGetComponent<MobFilterComponent>(Owner, out var comp))
            _window.SelectValues(comp.States);
        _window.OnToggle += state => SendPredictedMessage(new MobFilterToggleMessage(state));
    }
}
