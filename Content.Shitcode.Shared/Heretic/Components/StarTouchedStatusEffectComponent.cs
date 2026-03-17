using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shitcode.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StarTouchedStatusEffectComponent : Component
{
    [DataField]
    public TimeSpan SleepTime = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId CosmicCloud = "EffectCosmicCloud";
}
