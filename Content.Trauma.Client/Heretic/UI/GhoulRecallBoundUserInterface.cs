// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Ui;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed partial class GhoulRecallBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GhoulRecallWindow _window = new();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GhoulRecallWindow>();
        _window.OnClose += Close;
        _window.OnItemSelected += SendMessage;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not HereticGhoulRecallUiState recallState)
            return;

        _window.Populate(recallState.Ghouls);
    }

    private void SendMessage(NetEntity ghoul)
    {
        SendPredictedMessage(new HereticGhoulRecallMessage(ghoul));
    }
}
