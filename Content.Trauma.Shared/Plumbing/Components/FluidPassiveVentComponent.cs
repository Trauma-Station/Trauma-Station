// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidPassiveVentComponent : Component
{
    [DataField("inlet")]
    public string InletName = "fluid";

    [DataField]
    public float TransferRate = 10f; // units per second
}
