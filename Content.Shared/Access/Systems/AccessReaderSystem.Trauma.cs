// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
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

    /// <summary>
    /// Sets the displayed access lists on an AccessReaderComponent and dirties it.
    /// Used by systems that need to swap displayed access without owning the component,
    /// ex: BorgDisguiseSystem.
    /// </summary>
    public void SetAccessListsOriginal(Entity<AccessReaderComponent> ent, List<HashSet<ProtoId<AccessLevelPrototype>>> lists)
    {
        ent.Comp.AccessListsOriginal = lists;
        Dirty(ent);
    }
}

/// <summary>
/// Raised on an entity with an AccessReaderComponent to let other systems
/// (ex: disguises) override the access lists shown when the entity's access is examined.
/// If OverrideAccessLists is left null, the component's real AccessListsOriginal is used.
/// </summary>
[ByRefEvent]
public record struct GetAccessReaderDisplayEvent(List<HashSet<ProtoId<AccessLevelPrototype>>>? OverrideAccessLists = null);
