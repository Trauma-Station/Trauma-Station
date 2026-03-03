using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Tackle;

/// <summary>
/// This component doesn't make user tackle by default, they still need special equipment.
/// Unless they also have <see cref="TackleModifierComponent"/>
/// </summary>
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
