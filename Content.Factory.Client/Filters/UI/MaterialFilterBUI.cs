// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Filters;

namespace Content.Factory.Client.Filters.UI;

public sealed class MaterialFilterBUI(EntityUid uid, Enum key) : BoundUserInterface(uid, key)
{
    private MaterialFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<MaterialFilterComponent>(Owner, out var comp))
            return;

        _window = this.CreateWindow<MaterialFilterWindow>();
        _window.SetComp(comp);
        _window.OnInvert += () => SendPredictedMessage(new MaterialFilterInvertMessage());
        _window.OnToggle += material => SendPredictedMessage(new MaterialFilterToggleMessage(material));
    }
}
