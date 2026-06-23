// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.Bitrunning;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Orion.Bitrunning.UI.Disk;

[UsedImplicitly]
public sealed class BitrunningDiskBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BitrunningDiskWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BitrunningDiskWindow>();
        _window.OnSelected += option => SendPredictedMessage(new BitrunningDiskSelectOptionMessage(option));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BitrunningDiskBoundUiState cast || _window == null)
            return;

        _window.SetState(cast.Options, cast.SelectedOption);
    }
}
