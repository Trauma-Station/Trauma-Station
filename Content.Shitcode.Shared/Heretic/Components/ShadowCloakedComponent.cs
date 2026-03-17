using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowCloakedComponent : Component
{
    [ViewVariables]
    public bool WasVisible = true;

    [DataField]
    public EntProtoId ShadowCloakEntity = "ShadowCloakEntity";
}
