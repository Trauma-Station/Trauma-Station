// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.AlertLevel;

namespace Content.Trauma.Shared.Lockers;

[RegisterComponent, NetworkedComponent, Access(typeof(StationAlertLevelLockSystem))]
[AutoGenerateComponentState]
public sealed partial class StationAlertLevelLockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public bool Locked = true;

    [DataField]
    public HashSet<ProtoId<AlertLevelPrototype>> LockedAlertLevels = new();

    [DataField, AutoNetworkedField]
    public EntityUid? StationId;
}
