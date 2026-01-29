using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.BloodSplatter;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class BloodSplattererComponent : Component
{
    [DataField]
    public EntProtoId Decal = new ("DecalSpawnerBloodSplattersTrauma");

    [DataField]
    public EntProtoId GibbedDecal = new ("DecalSpawnerGibBloodSplatters");

    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 5;

    [DataField]
    public float Chance = .05f;

    [DataField]
    public TimeSpan SplashCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextSplashAvailable;
}
