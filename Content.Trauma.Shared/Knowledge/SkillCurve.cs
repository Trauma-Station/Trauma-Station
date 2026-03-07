// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Knowledge;

/// <summary>
/// A skill curve which takes skills from 0 to 100 and outputs a float around 1 by default.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class SkillCurve
{
    /// <summary>
    /// Multiplies the skill value before applying it to the curve function.
    /// Applied before <see cref="SkillOffset"/>
    /// </summary>
    [DataField]
    public float SkillScale = 1f;

    /// <summary>
    /// Offset applied to the skill before it is applied to the curve function.
    /// Applied after <see cref="SkillScale"/>.
    /// </summary>
    [DataField]
    public float SkillOffset;

    /// <summary>
    /// Multiplies the value of the curve function by this constant.
    /// Applied before <see cref="CurveOffset"/>.
    /// </summary>
    [DataField]
    public float CurveScale = 1f;

    /// <summary>
    /// Offsets the value of the curve function by this constant.
    /// Applied after <see cref="CurveScale"/>.
    /// </summary>
    [DataField]
    public float CurveOffset;

    public float GetCurve(int skill)
    {
        // skill is always within [0, 1] before any variables are applied
        var x = (0.01f * skill) * SkillScale + SkillOffset;
        var y = GetValue(x);
        return y * CurveScale + CurveOffset;
    }

    /// <summary>
    /// Get the y value for a given x value.
    /// By default x is within [0, 1]
    /// </summary>
    protected abstract float GetValue(float x);
}

/// <summary>
/// A straight line y = x
/// </summary>
public sealed partial class LinearSkillCurve : SkillCurve
{
    protected override float GetValue(float x)
        => x;
}

/// <summary>
/// A curve using the graph x^2
/// Starts off gradual but gets steeper as x approaches InverseScale.
/// Increases on either side when x is offset.
/// </summary>
public sealed partial class QuadraticSkillCurve : SkillCurve
{
    protected override float GetValue(float x)
        => x * x;
}

/// <summary>
/// A curve using the graph x^3
/// Can rapidly increase but also change directions.
/// </summary>
public sealed partial class CubicSkillCurve : SkillCurve
{
    protected override float GetValue(float x)
        => x * x * x;
}
