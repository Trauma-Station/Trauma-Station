// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Body;

/// <summary>
/// Organ that adds to base movement speed values while enabled.
/// Not required for movement.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpeedModifierOrganComponent : Component
{
    [DataField]
    public float WeightlessAcceleration;

    // add more if you want to use them :)
}
