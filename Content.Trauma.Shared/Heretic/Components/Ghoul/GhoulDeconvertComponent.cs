using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulDeconvertComponent : Component
{
    [DataField]
    public float Delay = 30f;
}
