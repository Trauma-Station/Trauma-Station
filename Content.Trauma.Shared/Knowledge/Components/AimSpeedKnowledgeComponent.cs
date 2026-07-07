// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Scales gun aim speed based on this knowledge's level.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ShootingKnowledgeSystem))]
public sealed partial class AimSpeedKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to multiply gun aim speed by.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
