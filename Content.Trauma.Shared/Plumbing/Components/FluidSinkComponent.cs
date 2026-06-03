// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidSinkComponent : Component
{
    [DataField]
    public string InletName = "faucet";

    [DataField]
    public string OutletName = "drain";

    [DataField]
    public FixedPoint2 FlowRate = FixedPoint2.New(10);

    [DataField]
    public FixedPoint2 DrainRate = FixedPoint2.New(10);
}
