using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Mimery;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActionRequiresEmptyHandComponent : Component
{
    [DataField]
    public LocId? PopupMessage;
}
