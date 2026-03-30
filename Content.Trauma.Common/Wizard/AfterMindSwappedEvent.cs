using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Wizard;

[Serializable, NetSerializable]
public record struct AfterMindSwappedEvent(EntityUid Performer, EntityUid Target);
