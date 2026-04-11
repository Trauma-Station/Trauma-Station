// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Goobstation.Common.StationReport;

public sealed class CommonNtrStationReportSystem : EntitySystem
{
    //stores the last received station report
    public string? StationReportText { get; private set; } = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<NtrStationReportEvent>(OnStationReportReceived);
    }

    private void OnStationReportReceived(NtrStationReportEvent ev)
    {
        // HOLY SHITCODE
        //Save the received message in the variable
        StationReportText = ev.StationReportText;
    }
}
