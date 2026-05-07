// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Paper;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Server.Station;

/// <summary>
/// Creates the station report and sends it to all comms consoles on the station.
/// </summary>
public sealed class StationReportSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly StationTraitsSystem _traits = default!;

    private StringBuilder _sb = new();
    private int _years;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationReportComponent, MapInitEvent>(OnMapInit);

        Subs.CVar(_cfg, TraumaCVars.YearOffset, y => _years = y, true);
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

            // TODO: custom greenshift/threat level announcement
            _chat.DispatchStationAnnouncement(station,
                "A summary of the station's situation has been copied and printed to all communications consoles.",
                "Station Report");
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
        var date = DateTime.UtcNow.AddYears(_years).ToString("ddd, MMM dd, yyyy");
        _sb.AppendLine($"[bolditalic]Nanotrasen Department of Intelligence Threat Advisory, Sol Sector, TCD {date}:[/bolditalic]\n");

        // TODO: actual dynamic gamemode reports lol
        _sb.AppendLine("Advisory Level: [bold]Yellow Star[/bold]");
        _sb.AppendLine("   Your sector's advisory level is Yellow Star.");
        _sb.AppendLine("   Surveillance shows a credible risk of enemy attack against our assets in the Sol Sector.");
        _sb.AppendLine("   We advise a heightened level of security alongside maintaining vigilance against potential threats.\n");

        // TODO: station goals

        _traits.AppendReport(_sb, station);

        _sb.AppendLine($"\n\n[italic]This advisory is intended for the staff of {Name(station)}. If this is not your station, you must destroy this document immediately.[/italic]");

        return _sb.ToString();
    }

    /// <summary>
    /// Spawn a copy of the report for a console.
    /// </summary>
    public void SpawnReport(EntityUid uid, EntProtoId proto, string text)
    {
        var coords = Transform(uid).Coordinates;
        var report = Spawn(proto, coords);
        _paper.SetContent(report, text);
    }
}
