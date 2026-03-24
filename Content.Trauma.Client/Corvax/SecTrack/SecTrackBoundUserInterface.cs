// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.SecTrack;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Corvax.SecTrack;

[UsedImplicitly]
public sealed class SecTrackBoundUserInterface : BoundUserInterface
{
    private SecTrackWindow? _window;

    public SecTrackBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SecTrackWindow>();

        _window.OnCreateSquad += squadName =>
            SendMessage(new CreateSquadMessage(squadName));

        _window.OnChangeSquadIcon += (squadId, iconId) =>
            SendMessage(new ChangeSquadIconMessage(squadId, iconId));

        _window.OnRenameSquad += (squadId, newName) =>
            SendMessage(new RenameSquadMessage(squadId, newName));

        _window.OnDeleteSquad += squadId =>
            SendMessage(new DeleteSquadMessage(squadId));

        _window.OnUpdateSquadDescription += (squadId, description) =>
            SendMessage(new UpdateSquadDescriptionMessage(squadId, description));

        _window.OnChangeSquadStatus += (squadId, status) =>
            SendMessage(new ChangeSquadStatusMessage(squadId, status));

        _window.OnAddMemberToSquad += (squadId, memberId) =>
            SendMessage(new AddMemberToSquadMessage(squadId, memberId));

        _window.OnRemoveMemberFromSquad += (squadId, memberId) =>
            SendMessage(new RemoveMemberFromSquadMessage(squadId, memberId));

        _window.OnRemoveTimer += timerUid =>
            SendMessage(new RemoveTimerMessage(timerUid));

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        switch (state)
        {
            case SecTrackUpdateState updateState:
                _window.UpdateState(updateState);
                break;

            case SensorStatusUpdateState sensorState:
                _window.UpdateSensorStatuses(sensorState);
                break;

            case TimerUpdateState timerState:
                _window.UpdateTimerState(timerState);
                break;
        }

    }
}
