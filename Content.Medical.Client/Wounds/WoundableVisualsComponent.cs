// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Wounds;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;

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
    public Dictionary<ProtoId<DamageGroupPrototype>, Color> DamageGroupColors = new()
    {
        { "Brute", Color.Red },
    };

    [DataField]
    public string? BleedingOverlay;

    /// <summary>
    /// List of damage sprite thresholds in descending order, largest threshold first.
    /// </summary>
    [DataField(required: true)]
    public List<FixedPoint2> Thresholds = [];

    /// <summary>
    /// Bleed sprite thresholds in descending order of bleeding amount.
    /// </summary>
    [DataField]
    public (BleedingSeverity, FixedPoint2)[] BleedingThresholds =
    [
        (BleedingSeverity.Severe, 7),
        (BleedingSeverity.Minor, 2.6)
    ];
}
