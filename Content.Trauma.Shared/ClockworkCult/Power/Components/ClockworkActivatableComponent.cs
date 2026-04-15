// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.ClockworkCult.Power.Components;

/// <summary>
/// All clockwork structures must have this component in order to activate.
///
/// An entity with this component will be activated based on some rules, such as:
/// 1. Are they anchored, or not?
/// 2. Do they have enough power, or not?
///
/// Requires <see cref="LimitedChargesComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ClockworkActivatableComponent : Component
{
    /// <summary>
    /// Whether this entity is active now
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// The components to add/remove on activation
    /// </summary>
    [DataField]
    public ComponentRegistry? ComponentsOnActivation;
}
