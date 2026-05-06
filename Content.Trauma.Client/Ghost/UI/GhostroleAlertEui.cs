// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;

namespace Content.Trauma.Client.Ghost.UI;

public sealed class GhostroleAlertEui : BaseEui
{
    private readonly GhostroleAlertMenu _menu;

    public GhostroleAlertEui()
    {
        _menu = new GhostroleAlertMenu();
    }

    public override void Opened()
    {
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _menu.Close();
    }
}
