// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Commands;

/// <summary>
/// Sent by client with Sent, sent back to server with both Sent and Received.
/// Used for measuring connection delay in both directions, accounting for message queues and what not.
/// </summary>
[Serializable, NetSerializable]
public sealed class CheckDelayEvent(TimeSpan sent) : EntityEventArgs
{
    public readonly TimeSpan Sent = sent;
    public TimeSpan Received;
}
