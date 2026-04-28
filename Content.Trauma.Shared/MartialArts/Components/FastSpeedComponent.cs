// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge;

namespace Content.Trauma.Shared.MartialArts.Components;

/// <summary>
/// Capeoria specific component for doing speed stuff.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FastSpeedComponent : Component
{
    /// <summary>
    /// Curve to multiply move speed with respect to level.
    /// Momentum also increases it afterwards.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve MoveCurve = default!;

    /// <summary>
    /// Curve to multiply when scaling melee damage with momentum.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve DamageScaleCurve = default!;
}
