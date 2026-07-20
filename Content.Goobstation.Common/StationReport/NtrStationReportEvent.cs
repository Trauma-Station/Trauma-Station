// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Common.StationReport;

[Serializable, NetSerializable]
public sealed class NtrStationReportEvent(string? text) : EntityEventArgs
{
    //This is where the stationreport is stored so the client can access it
    public readonly string? StationReportText = text;
}
