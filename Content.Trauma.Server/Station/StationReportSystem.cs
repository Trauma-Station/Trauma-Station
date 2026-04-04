// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Paper;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Trauma.Server.Station;

/// <summary>
/// Creates the station report and sends it to all comms consoles on the station.
/// </summary>
public sealed class StationReportSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedPaperSystem _paper = default!;

    private StringBuilder _sb = new();
    private int _year;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationReportComponent, MapInitEvent>(OnMapInit);

        Subs.CVar(_cfg, TraumaCVars.InGameYear, y => _year = y, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationReportComponent>();
        while (query.MoveNext(out var station, out var comp))
        {
            if (_timing.CurTime < comp.NextReport)
                continue;

            RemCompDeferred(station, comp);

            var text = CreateReport(station);
            var proto = comp.ReportProto;
            var consoles = EntityQueryEnumerator<StationReportTargetComponent>();
            while (consoles.MoveNext(out var uid, out _))
            {
                SpawnReport(uid, proto, text);
            }
        }
    }

    private void OnMapInit(Entity<StationReportComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextReport = _timing.CurTime + ent.Comp.ReportDelay;
    }

    /// <summary>
    /// Generate the station report text.
    /// </summary>
    public string CreateReport(EntityUid station)
    {
        _sb.Clear();
        return _sb.ToString();
    }

    /// <summary>
    /// Spawn a copy of the report for a console.
    /// </summary>
    public void SpawnReport(EntityUid uid, EntProtoId proto, string text)
    {
        var coords = Transform(uid).Coordinates;
        var report = Spawn(proto, coords);
        _paper.SetContents(report, text);
    }
}
