// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Wounds;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Medical.Client.Wounds;

[RegisterComponent]
public sealed partial class WoundableVisualsComponent : Component
{
    /// <summary>
    /// Dictionary of damage group to rsi path to find overlays in.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<DamageGroupPrototype>, string> DamageGroupSprites = new();

    /// <summary>
    /// Optional colors for certain damage groups.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, Color> DamageGroupColors = new();

    [DataField]
    public string? BleedingOverlay;

    [DataField(required: true)]
    public List<FixedPoint2> Thresholds = [];

    [DataField]
    public Dictionary<BleedingSeverity, FixedPoint2> BleedingThresholds = new()
    {
        { BleedingSeverity.Minor, 2.6 },
        { BleedingSeverity.Severe, 7 },
    };
}
