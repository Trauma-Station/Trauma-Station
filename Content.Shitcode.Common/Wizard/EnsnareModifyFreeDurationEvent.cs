using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Wizard;

/// <summary>
/// Raised on an entity to see if anything modifies it ensnare duration time to get out.
/// </summary>
[Serializable, NetSerializable]
public record struct EnsnareModifyFreeDurationEvent(EntityUid Target, float FreeTime);
