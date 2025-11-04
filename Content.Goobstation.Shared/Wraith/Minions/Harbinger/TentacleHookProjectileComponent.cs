using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Wraith.Minions.Harbinger;

[RegisterComponent, NetworkedComponent]
public sealed partial class TentacleHookProjectileComponent : Component
{
    [DataField]
    public TimeSpan DurationSlow = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId SlowdownEffect = "TentacleHookStatusEffect";

    [DataField]
    public EntityUid? Target;
}
