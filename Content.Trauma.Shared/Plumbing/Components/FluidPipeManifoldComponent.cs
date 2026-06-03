// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Plumbing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FluidPipeManifoldComponent : Component
{
    [DataField("inlets")]
    public HashSet<string> InletNames { get; set; } = new() { "south0", "south1", "south2" };

    [DataField("outlets")]
    public HashSet<string> OutletNames { get; set; } = new() { "north0", "north1", "north2" };
}
