// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Wounds;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Prototypes;

namespace Content.Medical.Shared.Wounds;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, fieldDeltas: true)]
[EntityCategory("Wounds")]
public sealed partial class WoundComponent : Component
{
    /// <summary>
    /// The organ this wound is applied to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid HoldingWoundable;

    /// <summary>
    /// Actual severity of the wound. The more the worse.
    /// Total amount dictates <see cref="WoundSeverity"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 WoundSeverityPoint;

    /// <summary>
    /// Damage group of this wound.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype>? DamageGroup;

    /// <summary>
    /// Damage type of this wound.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DamageTypePrototype> DamageType;

    /// <summary>
    /// Scar wound prototype, what will be spawned upon healing this wound.
    /// If null - no scar wound will be spawned.
    /// </summary>
    [DataField]
    public EntProtoId? ScarWound;

    /// <summary>
    /// Well, name speaks for this.
    /// </summary>
    [DataField]
    public bool IsScar;

    /// <summary>
    /// Wound severity. Has six severities: Healed/Minor/Moderate/Severe/Critical and Loss.
    /// Directly depends on <see cref="WoundSeverityPoint"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public WoundSeverity WoundSeverity;

    /// <summary>
    /// "Can be healed". Tend wounds surgery bypasses that
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBeHealed = true;

    /// <summary>
    /// Whether the wound can mangle its woundable, and at which severity.
    /// </summary>
    [DataField]
    public WoundSeverity? MangleSeverity;

    /// <summary>
    /// String of text used for displaying things about the wound in popups and self inspects.
    /// </summary>
    [DataField]
    public string? TextString;

    /// <summary>
    /// "Always show in inspects"
    /// </summary>
    [DataField]
    public bool AlwaysShowInInspects;
}
