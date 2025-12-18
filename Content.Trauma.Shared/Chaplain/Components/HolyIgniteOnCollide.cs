using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Chaplain.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolyIgniteOnCollideComponent : Component
{
    /// <summary>
    /// How many more times the ignition can be applied.
    /// </summary>
    [DataField]
    public int Count = 1;

    [DataField]
    public float FireStacks;

    [DataField]
    public string FixtureId = "ignition";

}
