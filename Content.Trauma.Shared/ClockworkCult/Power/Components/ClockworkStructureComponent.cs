// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Marker component used for connecting a battery (via <see cref="ClockwinderComponent"/>),
/// to an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClockworkStructureComponent : Component
{
    /// <summary>
    /// From where this entity is getting charges.
    /// </summary>
    [DataField]
    public EntityUid? Transferrer;
}
