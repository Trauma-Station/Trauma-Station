// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class FluidPassiveVentComponent : Component
{
    [DataField("inlet")]
    public string InletName = "pipe";

    [DataField]
    public float TransferRate = 10f; // units per second
}
