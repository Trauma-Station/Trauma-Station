// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.CartridgeLoader.Cartridges;
using Content.Trauma.Client.CartridgeLoader.Cartridges;
using Content.Trauma.Common.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface.Controllers;

namespace Content.DeltaV.Client.CartridgeLoader.Cartridges;

public sealed partial class LogProbeUiFragmentDeltaV : UIController
{
    public override void Initialize()
    {
        base.Initialize();

        LogProbeUiFragment.OnCreated += HookFragment;
    }

    private void HookFragment(LogProbeUiFragment window)
    {
        window.OnDisplayNanoChat = (state) => DisplayNanoChatData(window, state);
        window.OnSetupNanoChatView = (state) => SetupNanoChatView(window, state);
        window.OnSetupAccessLogView = () => SetupAccessLogView(window);
        window.OnDisplayAccessLogs = (state) => DisplayAccessLogs(window, state);
    }

    private void DisplayNanoChatData(LogProbeUiFragment window, NanoChatData data)
    {
        // First add a recipient list entry
        var recipientsList = Loc.GetString("log-probe-recipient-list") + "\n" + string.Join("\n",
            data.Recipients.Values
                .OrderBy(r => r.Name)
                .Select(r => $"    {r.Name}" +
                             (string.IsNullOrEmpty(r.JobTitle) ? "" : $" ({r.JobTitle})") +
                             $" | #{r.Number:D4}"));

        var recipientsEntry = new LogProbeUiEntry(0, "---", recipientsList);
        window.ProbedDeviceContainer.AddChild(recipientsEntry);

        var count = 1;
        foreach (var (partnerId, messages) in data.Messages)
        {
            // Show only successfully delivered incoming messages
            var incomingMessages = messages
                .Where(msg => msg.SenderId == partnerId && !msg.DeliveryFailed)
                .OrderByDescending(msg => msg.Timestamp);

            foreach (var msg in incomingMessages)
            {
                var messageText = Loc.GetString("log-probe-message-format",
                    ("sender", $"#{msg.SenderId:D4}"),
                    ("recipient", $"#{data.CardNumber:D4}"),
                    ("content", msg.Content));

                var entry = new NanoChatLogEntry(
                    count,
                    TimeSpan.FromSeconds(Math.Truncate(msg.Timestamp.TotalSeconds)).ToString(),
                    messageText);

                window.ProbedDeviceContainer.AddChild(entry);
                count++;
            }

            // Show only successfully delivered outgoing messages
            var outgoingMessages = messages
                .Where(msg => msg.SenderId == data.CardNumber && !msg.DeliveryFailed)
                .OrderByDescending(msg => msg.Timestamp);

            foreach (var msg in outgoingMessages)
            {
                var messageText = Loc.GetString("log-probe-message-format",
                    ("sender", $"#{msg.SenderId:D4}"),
                    ("recipient", $"#{partnerId:D4}"),
                    ("content", msg.Content));

                var entry = new NanoChatLogEntry(
                    count,
                    TimeSpan.FromSeconds(Math.Truncate(msg.Timestamp.TotalSeconds)).ToString(),
                    messageText);

                window.ProbedDeviceContainer.AddChild(entry);
                count++;
            }
        }
    }

    private void SetupNanoChatView(LogProbeUiFragment window, NanoChatData data)
    {
        window.TitleLabel.Text = Loc.GetString("log-probe-header-nanochat");
        window.ContentLabel.Text = Loc.GetString("log-probe-label-message");

        // Show card info if available
        var cardInfo = new List<string>();
        if (data.CardNumber != null)
            cardInfo.Add(Loc.GetString("log-probe-card-number", ("number", $"#{data.CardNumber:D4}")));

        // Add recipient count
        cardInfo.Add(Loc.GetString("log-probe-recipients", ("count", data.Recipients.Count)));

        window.CardNumberLabel.Text = string.Join(" | ", cardInfo);
        window.CardNumberLabel.Visible = true;
    }

    private void SetupAccessLogView(LogProbeUiFragment window)
    {
        window.TitleLabel.Text = Loc.GetString("log-probe-header-access");
        window.ContentLabel.Text = Loc.GetString("log-probe-label-accessor");
        window.CardNumberLabel.Visible = false;
    }

    private void DisplayAccessLogs(LogProbeUiFragment window, List<PulledAccessLog> logs)
    {
        //Reverse the list so the oldest entries appear at the bottom
        logs.Reverse();

        var count = 1;
        foreach (var log in logs)
        {
            window.AddAccessLog(log, count);
            count++;
        }
    }
}
