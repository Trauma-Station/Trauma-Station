using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

[RegisterComponent]
public sealed partial class PredictedProjectileClientComponent : Component
{
    [DataField]
    public bool Hit;

    [DataField]
    public EntityCoordinates? Coordinates;

    [DataField]
    public bool IgnoreShooter = true;

    [DataField]
    public bool DeleteOnCollide = true;

    [DataField]
    public bool OnlyCollideWhenShot;

    [DataField]
    public bool DamagedEntity;

    [DataField]
    public bool ProjectileSpent;

    [DataField]
    public FixedPoint2 PenetrationThreshold = FixedPoint2.Zero;

    [DataField]
    public List<string>? PenetrationDamageTypeRequirement;

    [DataField]
    public FixedPoint2 PenetrationAmount = FixedPoint2.Zero;
}
