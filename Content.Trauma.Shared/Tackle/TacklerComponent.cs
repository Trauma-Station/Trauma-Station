using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Tackle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TacklerComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextTackle;

    [DataField]
    public TimeSpan TackleCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    [DataField]
    public float Range = 3f;

    [DataField]
    public float Speed = 10f;

    [DataField]
    public float MinDistance;

    [DataField]
    public float StaminaCost = 25f;
}
