// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface;

namespace Content.Trauma.Client.ClockworkCult.UI;

public sealed class ClockworkSlabBUI : BoundUserInterface
{
    [ViewVariables]
    private ClockWorkSlabWindow? _window;

    public ClockworkSlabBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ClockWorkSlabWindow>();
        _window.SetOwner(Owner);
        _window.OpenCenteredLeft();
    }
}
