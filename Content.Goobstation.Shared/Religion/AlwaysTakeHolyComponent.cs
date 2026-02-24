using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Religion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlwaysTakeHolyComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ShouldBibleSmite = true;
}
