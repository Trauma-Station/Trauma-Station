using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Religion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShouldTakeHolyComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Sources = new();
}
