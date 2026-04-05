// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class FluidVentComponent : Component
{
    [DataField("inlet")]
    public string InletName = "fluid";

    [DataField]
    public float TransferRate = 20f; // units per second

    [DataField]
    public float MaxPushPressure = 3.0f; // Can overfill pipes significantly
}
