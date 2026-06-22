// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Lockers;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationAlertLevelLockSystem))]
[AutoGenerateComponentState]
public sealed partial class StationAlertLevelLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField]
    public HashSet<string> LockedAlertLevels = new();

    [DataField, AutoNetworkedField]
    public EntityUid? StationId;
}
