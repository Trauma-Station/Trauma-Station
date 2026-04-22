// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Skills.Systems;
using Robust.Shared.GameStates;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Skills.Components;

/// <summary>
/// Adds bonus damage to your melee attacks using weapons or punching.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeSkillSystem))]
public sealed partial class MeleeDamageSkillComponent : Component
{
    /// <summary>
    /// The curve to multiply damage by, this gets multiplied so it should not be 0.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
