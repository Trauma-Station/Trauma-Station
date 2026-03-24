using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Crucible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class XRayVisionComponent : Component
{
    [DataField]
    public bool EyeHadFov;
}
