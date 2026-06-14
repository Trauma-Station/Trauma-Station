// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Body.Chips;

/// <summary>
/// Adds and removes components from the body this organ chip is installed into.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganChipComponentsComponent : Component
{
    /// <summary>
    /// When attached, the chip will ensure these components on the entity, and delete them on removal.
    /// </summary>
    [DataField]
    public ComponentRegistry? OnAdd;

    /// <summary>
    /// When removed, the chip will ensure these components on the entity, and delete them on insertion.
    /// </summary>
    [DataField]
    public ComponentRegistry? OnRemove;
}
