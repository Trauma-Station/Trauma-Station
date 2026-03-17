using Robust.Shared.GameStates;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshMimickedComponent : Component
{
    [DataField]
    public List<EntityUid> FleshMimics = new();
}
