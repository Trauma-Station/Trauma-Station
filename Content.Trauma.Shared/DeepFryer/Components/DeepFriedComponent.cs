using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.DeepFryer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeepFriedComponent : Component
{
    public Color OriginalColor = new ();

    [DataField]
    public Color DeepFriedColor = new (212, 159, 52);
}
