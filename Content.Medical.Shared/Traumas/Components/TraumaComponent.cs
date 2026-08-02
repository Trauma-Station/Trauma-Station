// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Medical.Shared.Traumas;

[RegisterComponent, NetworkedComponent]
[EntityCategory("Traumas")]
[AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class TraumaComponent : Component
{
    /// <summary>
    /// Part this trauma belongs to, can be null if the organ or bone, etc; got delimbed but still exists
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? HoldingWoundable;

    /// <summary>
    /// For OrganDamage - the organ
    /// For BoneDamage - the bone
    /// For Dismemberment - the parent woundable, of the woundable that got delimbed
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TraumaTarget;

    /// <summary>
    /// The wound this trauma was applied by.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Wound;

    /// <summary>
    /// The severity the wound had when trauma got induced; Gets updated to the new one if the trauma gets worsened by the same wound
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 TraumaSeverity;

    [DataField, AutoNetworkedField]
    public TraumaType TraumaType;
}
