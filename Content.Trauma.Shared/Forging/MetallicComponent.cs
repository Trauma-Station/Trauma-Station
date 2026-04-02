// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Component for items that are fully made out of 1 metal with uniform properties.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedMetalSystem))]
[AutoGenerateComponentState]
public sealed partial class MetallicComponent : Component
{
    /// <summary>
    /// Temperature at which the metal stops being soft enough to work on.
    /// </summary>
    [DataField(required: true)]
    public float MinTemp;

    /// <summary>
    /// Ideal temperature for working on this metal.
    /// </summary>
    [DataField(required: true)]
    public float IdealTemp;

    /// <summary>
    /// Once outside temperature is this hot, you will take damage from holding it.
    /// </summary>
    [DataField]
    public float DamageHoldingTemp = 353.15f;

    /// <summary>
    /// Whether it is currently workable.
    /// Set when it is heated up to <see cref="IdealTemp"/>, unset when cooled below <see cref="MinTemp"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Workable;
}

/// <summary>
/// Raised on a metal when workable changes.
/// </summary>
[ByRefEvent]
public record struct MetalWorkableChangedEvent(bool Workable);
