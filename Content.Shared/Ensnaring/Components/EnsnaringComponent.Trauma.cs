using Content.Shared.Whitelist;

namespace Content.Shared.Ensnaring.Components;

/// <summary>
/// Trauma - extra fields
/// </summary>
public sealed partial class EnsnaringComponent
{
    /// <summary>
    /// Should the ensaring entity be deleted upon removal?
    /// </summary>
    [DataField]
    public bool DestroyOnRemove;

    /// <summary>
    /// Entities which the bola will pass through.
    /// </summary>
    [DataField]
    public EntityWhitelist? IgnoredTargets;
}
