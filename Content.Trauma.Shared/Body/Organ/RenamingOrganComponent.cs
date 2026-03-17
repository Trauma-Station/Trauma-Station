// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Body.Organ;

/// <summary>
/// Causes this organ to rename itself when installed into a body.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RenamingOrganSystem))]
public sealed partial class RenamingOrganComponent : Component
{
    /// <summary>
    /// Loc string to change the entity name to.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;
}
