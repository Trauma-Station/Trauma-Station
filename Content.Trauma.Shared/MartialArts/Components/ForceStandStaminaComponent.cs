// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Scales the stamina cost of forcing yourself upright while knocked down.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ForceStandStaminaComponent : Component
{
    /// <summary>
    /// Multiplier applied to the stamina cost. 0 makes standing up free.
    /// </summary>
    [DataField]
    public float Multiplier;
}
