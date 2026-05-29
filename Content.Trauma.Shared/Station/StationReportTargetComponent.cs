// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Station;

/// <summary>
/// Given to the crews comms console to let station reports spawn on it.
/// Not using comms console component since aghosts have that...
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationReportTargetComponent : Component;
