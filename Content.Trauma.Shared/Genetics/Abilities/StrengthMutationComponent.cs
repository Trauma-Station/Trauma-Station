// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Genetics.Abilities;

/// <summary>
/// Component for the strength mutation.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrengthMutationSystem))]
public sealed partial class StrengthMutationComponent : Component
{
    /// <summary>
    /// Modifier for melee damage dealt.
    /// </summary>
    [DataField]
    public float MeleeModifier = 1.2f;
}
