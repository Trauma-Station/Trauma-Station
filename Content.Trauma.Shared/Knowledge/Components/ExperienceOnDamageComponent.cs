// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Knowledge component to gain XP whenever you take damage from some "real" source,
/// e.g. getting hit as opposed to radiation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ExperienceOnDamageSystem))]
public sealed partial class ExperienceOnDamageComponent : Component
{
    /// <summary>
    /// XP comes from the attack damage divided by this number.
    /// </summary>
    [DataField]
    public int DamageScale = 5;

    /// <summary>
    /// Maximum XP to gain in a single attack.
    /// </summary>
    [DataField]
    public int MaxGain = 10;
}
