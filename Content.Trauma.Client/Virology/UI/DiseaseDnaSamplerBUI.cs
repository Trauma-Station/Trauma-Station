// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Virology;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Virology.UI;

public sealed class DiseaseDnaSamplerBUI : BoundUserInterface
{
    private DiseaseDnaSamplerWindow? _window;

    public DiseaseDnaSamplerBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DiseaseDnaSamplerWindow>();
        _window.SetOwner(Owner);
        _window.OnCreateInjector += () => SendPredictedMessage(new DiseaseDnaSamplerCreateMessage());
    }
}
