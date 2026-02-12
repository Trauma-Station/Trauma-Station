// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Medical.Common.Body;

/// <summary>
/// Adds and removes components from the body this organ is part of.
/// Tracks which components already existed to be less bug-prone.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class OrganComponentsComponent : Component
{
    /// <summary>
    /// Components which were added.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> AddedKeys = new();

    /// <summary>
    /// When attached, the organ will ensure these components on the entity, and delete them on removal.
    /// </summary>
    [DataField]
    public ComponentRegistry? OnAdd;

    /// <summary>
    /// When removed, the organ will ensure these components on the entity, and delete them on insertion.
    /// </summary>
    [DataField]
    public ComponentRegistry? OnRemove;
}
