namespace Content.Trauma.Shared.Chaplain.Components;

[RegisterComponent]
public sealed partial class HolyIgniteOnCollideComponent : Component
{
    /// <summary>
    /// How many more times the ignition can be applied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("count")]
    public int Count = 1;

    [ViewVariables(VVAccess.ReadWrite), DataField("fireStacks")]
    public float FireStacks;

    [ViewVariables(VVAccess.ReadWrite), DataField("fixtureId")]
    public string FixtureId = "ignition";

}
