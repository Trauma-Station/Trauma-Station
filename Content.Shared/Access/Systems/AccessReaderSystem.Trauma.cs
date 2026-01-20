using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

/// <summary>
/// Trauma - API additions for access checking
/// </summary>
public sealed partial class AccessReaderSystem
{
    /// <summary>
    /// Returns true if the user has a given access level.
    /// </summary>
    public bool UserHasAccess(EntityUid user, ProtoId<AccessLevelPrototype> level)
        => FindAccessTags(user).Contains(level);
}
