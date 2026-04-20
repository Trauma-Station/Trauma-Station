// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.StationReport;
using Content.Server.GameTicking;
using Content.Shared.Paper;
using Robust.Shared.GameObjects;

namespace Content.Goobstation.Server.StationReport;

// TODO: make this a component on the station entity bruh
public sealed class NtrStationReportSystem : EntitySystem
{

    //this is shitcode? yes it is

    public override void Initialize()
    {
        //subscribes to the endroundevent
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    private void OnRoundEndTextAppend(RoundEndTextAppendEvent args)
    {
        //locates the first entity with StationReportComponent then stops
        string? stationReportText = null;
        var query = EntityQueryEnumerator<NtrStationReportComponent>();
        while (query.MoveNext(out var uid, out var tablet))//finds the first entity with stationreport
        {
            if (!TryComp<PaperComponent>(uid, out var paper))
               return;

            stationReportText = paper.Content;
            break;
        }
        BroadcastStationReport(stationReportText);
    }

    //sends a networkevent to tell the client to update the stationreporttext when recived
    public void BroadcastStationReport(string? stationReportText)
    {
        RaiseNetworkEvent(new NtrStationReportEvent(stationReportText));//to send to client
    }
}
