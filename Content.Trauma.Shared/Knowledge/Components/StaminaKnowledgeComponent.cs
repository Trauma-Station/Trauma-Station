// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Modifies stamina damage taken and gives XP when taking stamina damage.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StaminaKnowledgeSystem))]
public sealed partial class StaminaKnowledgeComponent : Component
{
    /// <summary>
    /// The curve to multiply stamina damage by, this gets multiplied with so it should not be 0.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;

    /// <summary>
    /// How much stamina damage is needed for 1 xp.
    /// </summary>
    [DataField]
    public int DamageScale = 5;

    /// <summary>
    /// Max XP to gain from losing stamina at a given time.
    /// </summary>
    [DataField]
    public int MaxGain = 10;
}
