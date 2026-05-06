// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Commands;

/// <summary>
/// Sent by client with Sent, sent back to server with both Sent and Received.
/// Used for measuring connection delay in both directions, accounting for message queues and what not.
/// </summary>
[Serializable, NetSerializable]
public sealed class CheckDelayEvent(DateTime sent) : EntityEventArgs
{
    public readonly DateTime Sent = sent;
    public DateTime Received;
}
