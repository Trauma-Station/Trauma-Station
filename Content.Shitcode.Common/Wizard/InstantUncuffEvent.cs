using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Wizard;

/// <summary>
/// Raised on the user to see if it can uncuff instantly.
/// </summary>
[Serializable, NetSerializable]
public record struct InstantUncuffEvent(EntityUid Target, EntityUid Cuff, bool CuffsBroken = false);
