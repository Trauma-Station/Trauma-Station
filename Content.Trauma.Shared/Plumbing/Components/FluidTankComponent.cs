// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Plumbing;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidTankComponent : Component
{
    /// <summary>
    /// Name for solution container and fluid node.
    /// </summary>
    [DataField]
    public string IntakeName = "tank";

    /// <summary>
    /// How much can a player transfer per click?
    /// </summary>
    [DataField] public FixedPoint2 TransferAmount = 100;
}
