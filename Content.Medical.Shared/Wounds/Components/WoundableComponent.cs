// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Wounds;
using Content.Medical.Common.Traumas;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Medical.Shared.Wounds;

/// <summary>
/// Component for bodyparts that can get wounded when taking damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class WoundableComponent : Component
{
    /// <summary>
    /// Indicates whether wounds are allowed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowWounds = true;

    /// <summary>
    /// Integrity points of this woundable.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 IntegrityCap;

    /// <summary>
    /// Integrity points of this woundable.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public FixedPoint2 Integrity;

    /// <summary>
    /// yeah
    /// </summary>
    [DataField(required: true)]
    public Dictionary<WoundableSeverity, FixedPoint2> Thresholds = new();

    /// <summary>
    /// How much the woundable is bleeding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Bleeds = FixedPoint2.Zero;

    [DataField]
    public SoundSpecifier WoundableDestroyedSound = new SoundCollectionSpecifier("WoundableDestroyed");

    [DataField]
    public SoundSpecifier WoundableDelimbedSound = new SoundCollectionSpecifier("WoundableDelimbed");

    /// <summary>
    /// State of the woundable. Severity basically.
    /// </summary>
    [DataField, AutoNetworkedField]
    public WoundableSeverity WoundableSeverity;

    /// <summary>
    /// Container potentially holding wounds.
    /// </summary>
    [ViewVariables]
    public Container Wounds = default!;

    /// <summary>
    /// Whether this woundable can be removed from a body..
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanRemove = true;

    /// <summary>
    /// Whether this woundable can bleed or not..
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBleed = true;

    /// <summary>
    /// Damage to inflict on the root when the woundable is amputated.
    /// </summary>
    [DataField]
    public DamageSpecifier? DamageOnAmputate;

    [DataField]
    public Dictionary<TraumaType, FixedPoint2> TraumaDeductions = new()
    {
        {TraumaType.Dismemberment, 0.3f},
    };
}
