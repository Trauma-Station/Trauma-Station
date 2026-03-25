// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.CartridgeLoader.Cartridges;
using Content.DV.Client.CartridgeLoader.Cartridges;
using Content.DV.Common.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface.Controllers;

namespace Content.DeltaV.Client.CartridgeLoader.Cartridges;

public sealed partial class LogProbeUiFragmentDeltaV : UIController
{
    private void OnWindowOpened(LogProbeUiFragment window)
    {
        window.OnDisplayNanoChat = (state) => DisplayNanoChatData(window, state);
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
}
