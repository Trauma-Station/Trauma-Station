using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Augments;

/// <summary>
/// Component that indicates an entity is an augment
/// The augmented body can be retrieved with <see cref="AugmentSystem.GetBody"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AugmentComponent : Component;

/// <summary>
/// Component that tracks which augments are installed on this body
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InstalledAugmentsComponent : Component
{
    /// <summary>
    /// The augments that are installed.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> InstalledAugments = new();
}
