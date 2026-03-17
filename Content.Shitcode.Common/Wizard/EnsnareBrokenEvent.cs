using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Wizard;

/// <summary>
/// Raised on the user whenever the user gets out of a snare.
/// </summary>
[Serializable, NetSerializable]
public record struct EnsnareBrokenEvent(EntityUid? Target);
