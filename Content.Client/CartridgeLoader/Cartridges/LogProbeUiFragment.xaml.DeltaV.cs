// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.DV.Common.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class LogProbeUiFragment
{
    public Action<NanoChatData>? OnDisplayNanoChat;

    private void SetupNanoChatView(NanoChatData data)
    {
        TitleLabel.Text = Loc.GetString("log-probe-header-nanochat");
        ContentLabel.Text = Loc.GetString("log-probe-label-message");

        // Show card info if available
        var cardInfo = new List<string>();
        if (data.CardNumber != null)
            cardInfo.Add(Loc.GetString("log-probe-card-number", ("number", $"#{data.CardNumber:D4}")));

        // Add recipient count
        cardInfo.Add(Loc.GetString("log-probe-recipients", ("count", data.Recipients.Count)));

        CardNumberLabel.Text = string.Join(" | ", cardInfo);
        CardNumberLabel.Visible = true;
    }

    private void SetupAccessLogView()
    {
        TitleLabel.Text = Loc.GetString("log-probe-header-access");
        ContentLabel.Text = Loc.GetString("log-probe-label-accessor");
        CardNumberLabel.Visible = false;
    }

    // DeltaV - Handle this in a separate method
    private void DisplayAccessLogs(List<PulledAccessLog> logs)
    {
        //Reverse the list so the oldest entries appear at the bottom
        logs.Reverse();

        var count = 1;
        foreach (var log in logs)
        {
            AddAccessLog(log, count);
            count++;
        }
    }
}
