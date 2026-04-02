using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Shared.Armor;

/// <summary>
/// Trauma - limb damage, trauma and AP related fields.
/// </summary>
public sealed partial class ArmorComponent
{
    /// <summary>
    /// Shitmed Change: If true, the coverage won't show.
    /// </summary>
    [DataField("coverageHidden")]
    public bool ArmourCoverageHidden;

    /// <summary>
    /// Shitmed Change: If true, the modifiers won't show.
    /// </summary>
    [DataField("modifiersHidden")]
    public bool ArmourModifiersHidden;

    /// <summary>
    /// Shitmed Change: thankfully all the armor in the game is symmetrical.
    /// </summary>
    [DataField("coverage")]
    public List<BodyPartType> ArmorCoverage = new();

    /// <summary>
    /// Shitmed Change: The amount of dismemberment chance deduction.
    /// </summary>
    [DataField]
    public Dictionary<TraumaType, FixedPoint2> TraumaDeductions = new()
    {
        { TraumaType.Dismemberment, 0 },
        { TraumaType.BoneDamage, 0 },
        { TraumaType.OrganDamage, 0 },
        { TraumaType.VeinsDamage, 0 },
        { TraumaType.NerveDamage, 0 },
    };
}
