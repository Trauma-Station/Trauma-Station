using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.BloodSplatter;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodSplattererComponent : Component
{
    [DataField]
    public string Colour = "#9900007F"; // Default red

    [DataField]
    public EntProtoId Entity = new EntProtoId("DecalSpawnerBloodSplattersTrauma");

    [DataField]
    public FixedPoint2 MinimalTriggerDamage = 5;

    [DataField]
    public float Chance = 0.5f;
}
