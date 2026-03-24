using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Crucible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NoClipComponent : Component
{
    [DataField]
    public LocId? ExamineLoc = "crucible-soul-effect-examine-message";
}
