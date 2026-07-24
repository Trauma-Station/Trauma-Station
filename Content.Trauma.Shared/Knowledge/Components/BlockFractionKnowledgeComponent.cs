// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Scales shield block fraction based knowledge's level.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockFractionKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to multiply fraction by
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
