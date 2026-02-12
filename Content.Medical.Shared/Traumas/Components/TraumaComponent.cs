using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Medical.Shared.Traumas;

[RegisterComponent, NetworkedComponent]
[EntityCategory("Traumas")]
public sealed partial class TraumaComponent : Component
{
    /// <summary>
    /// Self-explanatory. Can be null if the organ or bone, etc; got delimbed but still exists
    /// </summary>
    [DataField]
    public EntityUid? HoldingWoundable;

    /// <summary>
    /// Self-explanatory
    /// For OrganDamage - the organ
    /// For BoneDamage - the bone
    /// For VeinsDamage and NerveDamage - the woundable
    /// For Dismemberment - the parent woundable, of the woundable that got delimbed
    /// </summary>
    [DataField]
    public EntityUid? TraumaTarget;

    /// <summary>
    /// SHITCODE ALERT!!!!! This PURELY EXISTS FOR DELIMB TRAUMAS. I hate myself.
    /// </summary>
    [DataField]
    public (BodyPartType, BodyPartSymmetry)? TargetType;

    /// <summary>
    /// The severity the wound had when trauma got induced; Gets updated to the new one if the trauma gets worsened by the same wound
    /// </summary>
    [DataField]
    public FixedPoint2 TraumaSeverity;

    /// <summary>
    /// Self-explanatory
    /// </summary>
    [DataField]
    public TraumaType TraumaType;
}

// The networking on consciousness is rather silly.
[Serializable, NetSerializable]
public sealed class TraumaComponentState : ComponentState
{
    public NetEntity? HoldingWoundable;
    public NetEntity? TraumaTarget;
    public (BodyPartType, BodyPartSymmetry)? TargetType;
    public FixedPoint2 TraumaSeverity;
    public TraumaType TraumaType;
}
