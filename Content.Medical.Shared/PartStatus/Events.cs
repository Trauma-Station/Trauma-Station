using Robust.Shared.Serialization;

namespace Content.Medical.Shared.PartStatus;

/// <summary>
/// Message sent by the client when you click your part status alert.
/// Server will then send you a chat message with damage info.
/// </summary>
[Serializable, NetSerializable]
public sealed class GetPartStatusEvent : EntityEventArgs;
