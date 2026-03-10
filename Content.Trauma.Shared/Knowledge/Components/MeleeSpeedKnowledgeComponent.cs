// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Multiplies melee attack speed according to a skill curve.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeKnowledgeSystem))]
public sealed partial class MeleeSpeedKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to scale speed by, should never give 0 as it is for multiplying.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
