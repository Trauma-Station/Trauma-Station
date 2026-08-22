// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Strip.Components;

/// <summary>
/// Allows direct bag/pocket access verbs on this entity when being stripped.
/// Doafter delays scale with the target's state.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BagAccessComponent : Component
{
    /// <summary>
    /// Doafter delay when the target is alive and uncuffed.
    /// </summary>
    [DataField]
    public TimeSpan NormalDelay = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Doafter delay when the target is cuffed or in critical condition.
    /// </summary>
    [DataField]
    public TimeSpan CuffedOrCritDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Doafter delay when the target is dead.
    /// </summary>
    [DataField]
    public TimeSpan DeadDelay = TimeSpan.FromSeconds(1);
}
