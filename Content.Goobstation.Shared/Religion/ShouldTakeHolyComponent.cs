using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Religion;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShouldTakeHolyComponent : Component
{
    [ViewVariables]
    public HashSet<EntityUid> Sources = new();
}
