using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshMimickedComponent : Component
{
    [DataField]
    public List<EntityUid> FleshMimics = new();
}
