// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Adds bonus damage to your melee attacks using weapons or punching.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeKnowledgeSystem))]
public sealed partial class MeleeDamageKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to multiply damage by, this gets multiplied so it should not be 0.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
