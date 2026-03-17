using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VoidPrisonComponent : Component
{
    [DataField]
    public EntProtoId EndEffect = "EffectVoidPrisonEnd";
}
