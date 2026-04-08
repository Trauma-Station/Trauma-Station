// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Skills.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Skills.Components;

/// <summary>
/// Multiplies melee attack speed according to a skill curve.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeSkillSystem))]
public sealed partial class MeleeSpeedSkillComponent : Component
{
    /// <summary>
    /// The curve to scale speed by, should never give 0 as it is for multiplying.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
