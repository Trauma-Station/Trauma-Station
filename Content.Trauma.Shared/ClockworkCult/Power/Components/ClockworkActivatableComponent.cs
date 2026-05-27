// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

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
    /// The effects to add on activation.
    /// </summary>
    [DataField]
    public EntityEffect[]? ActivationEffects;

    /// <summary>
    /// The effects to add on de-activation.
    /// </summary>
    [DataField]
    public EntityEffect[]? DeactivationEffects;
}
