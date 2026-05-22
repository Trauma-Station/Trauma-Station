namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LionhunterRifleProjectileComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry ComponentsOnEmpower;

    [DataField]
    public float EmpowerDamageMultiplier = 2f;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public EntityUid? EmpowerTarget;

    [DataField, AutoNetworkedField]
    public HereticPath? ShooterPath;

    [DataField, AutoNetworkedField]
    public int ShooterPassiveLevel = 1;
}
