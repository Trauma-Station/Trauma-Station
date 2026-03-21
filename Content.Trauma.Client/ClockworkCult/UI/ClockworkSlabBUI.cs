// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.ClockworkCult.Scripture;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

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

        _window.OnRecite += WindowOnOnRecite;
    }

    private void WindowOnOnRecite(EntProtoId? scriptureProto)
    {
        if (scriptureProto is not { } scripture)
            return;

        SendPredictedMessage(new ScriptureReciteEvent(scripture));
    }
}
