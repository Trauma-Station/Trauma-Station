// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.SecTrack;

[Serializable, NetSerializable]
public enum SecTrackUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SecTrackUpdateState(
        string stationName,
        List<CrewMemberInfo> securityCrew,
        List<CrewMemberInfo> unassignedSecurity,
        List<Squad> squads) : BoundUserInterfaceState
{
    public string StationName = stationName;
    public List<CrewMemberInfo> SecurityCrew = securityCrew;
    public List<CrewMemberInfo> UnassignedSecurity = unassignedSecurity;
    public List<Squad> Squads = squads;
}

[Serializable, NetSerializable]
public sealed class SensorStatusUpdateState(
    Dictionary<string, SuitSensorStatus?> memberStatuses,
    Dictionary<string, (string Location, bool HasLocation)> squadLocations) : BoundUserInterfaceState
{
    public Dictionary<string, SuitSensorStatus?> MemberStatuses = memberStatuses;
    public Dictionary<string, (string Location, bool HasLocation)> SquadLocations = squadLocations;

}

[Serializable, NetSerializable]
public sealed class CreateSquadMessage(string squadName) : BoundUserInterfaceMessage
{
    public string SquadName = squadName;
}

[Serializable, NetSerializable]
public sealed class DeleteSquadMessage(string squadId) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
}

[Serializable, NetSerializable]
public sealed class RenameSquadMessage(string squadId, string newName) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public string NewName = newName;
}

[Serializable, NetSerializable]
public sealed class UpdateSquadDescriptionMessage(string squadId, string description) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public string Description = description;
}

[Serializable, NetSerializable]
public sealed class AddMemberToSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public string MemberId = memberId;
}

[Serializable, NetSerializable]
public sealed class RemoveMemberFromSquadMessage(string squadId, string memberId) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public string MemberId = memberId;
}

[Serializable, NetSerializable]
public sealed class ChangeSquadIconMessage(string squadId, int iconId) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public int IconId = iconId;
}

[Serializable, NetSerializable]
public sealed class ChangeSquadStatusMessage(string squadId, string status) : BoundUserInterfaceMessage
{
    public string SquadId = squadId;
    public string Status = status;
}

[Serializable, NetSerializable]
public sealed class TimerUpdateState(string timers) : BoundUserInterfaceMessage
{
    public string Timers = timers;

}

[Serializable, NetSerializable]
public sealed class RemoveTimerMessage(string timerUid) : BoundUserInterfaceMessage
{
    public string TimerUid = timerUid;
}
