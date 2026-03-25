// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Curses;
using Content.Trauma.Shared.Heretic.Curses.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed class CurseBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private CurseWindow _window = new();

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CurseWindow>();
        _window.OnClose += Close;
        _window.ButtonClicked += SendMessage;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not PickCurseVictimState curseState)
            return;

        if (!_entMan.TryGetComponent(Owner, out HereticCurseProviderComponent? provider))
            return;

        _window.UpdateData(curseState.Data, provider.CursePrototypes);
    }

    private void SendMessage(NetEntity victim, EntProtoId proto)
    {
        SendMessage(new CurseSelectedMessage(victim, proto));
    }
}
