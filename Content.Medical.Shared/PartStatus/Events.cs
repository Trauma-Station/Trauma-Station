// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Medical.Shared.PartStatus;

/// <summary>
/// Message sent by the client when you click your part status alert.
/// Server will then send you a chat message with damage info.
/// </summary>
[Serializable, NetSerializable]
public sealed class GetPartStatusEvent : EntityEventArgs;
