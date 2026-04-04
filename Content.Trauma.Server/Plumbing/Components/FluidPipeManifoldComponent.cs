namespace Content.Trauma.Server.Plumbing.Components;

[RegisterComponent]
public sealed partial class FluidPipeManifoldComponent : Component
{
    [DataField("inlets")]
    public HashSet<string> InletNames { get; set; } = new() { "south0", "south1", "south2" };

    [DataField("outlets")]
    public HashSet<string> OutletNames { get; set; } = new() { "north0", "north1", "north2" };
}
