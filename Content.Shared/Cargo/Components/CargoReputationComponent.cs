using Robust.Shared.GameStates;

namespace Content.Shared.Cargo.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CargoReputationComponent : Component
{
    [DataField("militaryRep")] public float MilitaryReputation = 0f;
    [DataField("medicalRep")] public float MedicalReputation = 0f;
    [DataField("serviceRep")] public float ServiceReputation = 0f;

    [DataField("militaryWants")] public List<string> MilitaryWants = new();
}
