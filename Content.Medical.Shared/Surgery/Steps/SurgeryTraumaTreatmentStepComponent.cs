using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Medical.Shared.Surgery.Steps;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTraumaTreatmentStepComponent : Component
{
    [DataField]
    public TraumaType TraumaType = TraumaType.BoneDamage;

    [DataField]
    public FixedPoint2 Amount = 5;
}
