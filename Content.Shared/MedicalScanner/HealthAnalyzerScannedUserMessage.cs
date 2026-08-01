// <Trauma>
using Content.Medical.Common.Wounds;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Trauma.Common.Medical.HealthAnalyzer;
using Robust.Shared.Prototypes;
// </Trauma>
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public HealthAnalyzerUiState State;

    public HealthAnalyzerScannedUserMessage(HealthAnalyzerUiState state)
    {
        State = state;
    }
}

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public struct HealthAnalyzerUiState
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    // <Trauma>
    public Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity>? Body;
    public HashSet<ProtoId<OrganCategoryPrototype>> Bleeding = new(); // per-part instead of global
    public FixedPoint2 VitalDamage;
    public NetEntity? Part;
    // </Traumaa>
    public bool? Unrevivable;

    public HealthAnalyzerUiState() {}

    public HealthAnalyzerUiState(NetEntity? targetEntity, float temperature, float bloodLevel, bool? scanMode,
        // <Trauma>
        HashSet<ProtoId<OrganCategoryPrototype>> bleeding,
        bool? unrevivable,
        Dictionary<ProtoId<OrganCategoryPrototype>, WoundableSeverity>? body,
        FixedPoint2 vitalDamage,
        NetEntity? part = null)
        // </Trauma>
    {
        // <Shitmed>
        Body = body;
        VitalDamage = vitalDamage;
        Part = part;
        // </Shitmed>
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
    }
}
