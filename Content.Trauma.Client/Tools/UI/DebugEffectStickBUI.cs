// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Tools;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Tools.UI;

public sealed class DebugEffectStickBUI : BoundUserInterface
{
    private DebugEffectStickWindow? _window;

    public DebugEffectStickBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DebugEffectStickWindow>();
        _window.SetOwner(Owner);
        _window.OnSetEffect += effect =>
        {
            SendPredictedMessage(new DebugStickSetEffectMessage(effect));
            Close();
        };
    }
}
