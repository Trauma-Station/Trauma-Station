using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Clothing.MultiToggleable;

[ByRefEvent]
public record struct ToggleClothingAttemptEvent(EntityUid Wearer, EntityUid User, EntityUid Clothing, bool Multiple, bool Cancelled = false);

[Serializable, NetSerializable]
public sealed partial class AttachClothingDoAfterEvent : SimpleDoAfterEvent;
