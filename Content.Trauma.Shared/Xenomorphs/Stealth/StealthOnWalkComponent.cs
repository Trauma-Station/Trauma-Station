// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Xenomorphs.Stealth;

[RegisterComponent, NetworkedComponent]
public sealed partial class StealthOnWalkComponent : Component
{
    [DataField]
    public FixedPoint2 PlasmaCost;

    [ViewVariables]
    public bool Stealth;
}
