using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Quality;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class QualityOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public float QualityOverride = 1;
}
