using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage;

/// <summary>
/// Trauma - add extra fields for wounding and armor penetration.
/// </summary>
public sealed partial class DamageSpecifier
{
    [DataField]
    public float ArmorPenetration;

    [DataField]
    public float PartDamageVariation;

    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> WoundSeverityMultipliers = new();

    public DamageSpecifier(float armorPenetration,
        float partVariation,
        Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> severityMultipliers)
    {
        ArmorPenetration = armorPenetration;
        PartDamageVariation = partVariation;
        WoundSeverityMultipliers = new (severityMultipliers);
    }

    /// <summary>
    /// Copy damage metadata (e.g. armor penetration) from the argument to this damage spec.
    /// </summary>
    public void CopyMetadata(DamageSpecifier src)
    {
        ArmorPenetration = src.ArmorPenetration;
        PartDamageVariation = src.PartDamageVariation;
        WoundSeverityMultipliers = new(src.WoundSeverityMultipliers);
    }

    /// <summary>
    /// Returns new armor modifier set for a given penetration value.
    /// </summary>
    public static DamageModifierSet PenetrateArmor(DamageModifierSet modifierSet, float penetration)
    {
        if (penetration == 0f ||
            penetration > 0f && (modifierSet.IgnoreArmorPierceFlags & (int) PartialArmorPierceFlags.Positive) != 0 ||
            penetration < 0f && (modifierSet.IgnoreArmorPierceFlags & (int) PartialArmorPierceFlags.Negative) != 0)
            return modifierSet;

        var result = new DamageModifierSet();
        if (penetration >= 1f)
            return result;

        var inversePen = 1f - penetration;

        foreach (var (type, coef) in modifierSet.Coefficients)
        {
            // Negative coefficients are not modified by this,
            // coefficients above 1 will actually be lowered which is not desired
            if (coef is <= 0 or >= 1)
            {
                result.Coefficients.Add(type, coef);
                continue;
            }

            result.Coefficients.Add(type, MathF.Pow(coef, inversePen));
        }

        foreach (var (type, flat) in modifierSet.FlatReduction)
        {
            // Negative flat reductions are not modified by this
            if (flat <= 0)
            {
                result.FlatReduction.Add(type, flat);
                continue;
            }

            result.FlatReduction.Add(type, flat * inversePen);
        }

        return result;
    }
}
