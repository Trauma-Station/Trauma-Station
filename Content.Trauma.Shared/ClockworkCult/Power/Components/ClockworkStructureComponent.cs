// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// Marker component used for connecting a battery (via <see cref="ClockwinderComponent"/>),
/// to an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkStructureComponent : Component
{
    /// <summary>
    /// From where this entity is getting charges.
    /// </summary>
    [DataField]
    public List<EntityUid?> Transferrers = new();

    /// <summary>
    /// Whether the structure is active now
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// The components to add/remove on activation
    /// </summary>
    [DataField, AutoNetworkedField]
    public ComponentRegistry? ComponentsOnActivation;
}
