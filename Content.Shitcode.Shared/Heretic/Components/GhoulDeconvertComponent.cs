using Robust.Shared.GameStates;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulDeconvertComponent : Component
{
    [DataField]
    public float Delay = 30f;
}
