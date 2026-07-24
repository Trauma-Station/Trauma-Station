using Content.Goobstation.Common.StationReport;
using Content.Goobstation.UIKit.UserInterface.Controls;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd;

public sealed partial class RoundEndSummaryWindow
{
    // TODO: make this shitcode injected instead of shitting this up
    private BoxContainer MakeStationReportTab()
    {
        var report = Loc.GetString("no-station-report-summited");
        //gets the stationreport varibible and sets the station report tab text to it if the map doesn't have a tablet will say No station report submitted
        var sys = _entityManager.System<CommonNtrStationReportSystem>();
        if (!string.IsNullOrWhiteSpace(sys.StationReportText) && sys.StationReportText != Loc.GetString("station-report-text"))
        {
            report = Loc.GetString(
                "station-report-end-round-text",
                ("bodytext", sys.StationReportText),
                ("roundid", RoundId)
            );
        }

        var tab = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Name = Loc.GetString("round-end-summary-window-station-report-tab-title")
        };
        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            Margin = new Thickness(10),
            HScrollEnabled = false,
        };
        var reportContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical
        };
        var label = new RichTextLabel();
        label.SetMarkup(report);
        reportContainer.AddChild(label);

        scroll.AddChild(reportContainer);
        tab.AddChild(scroll);
        return tab;
    }
}
