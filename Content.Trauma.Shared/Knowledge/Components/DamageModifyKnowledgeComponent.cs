// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component that modifies physical damage taken.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DamageModifyKnowledgeSystem))]
public sealed partial class DamageModifyKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to multiply damage by, this gets multiplied with so it should not be 0.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
