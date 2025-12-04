// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Damage;

/// <summary>
/// Modifies incoming damage from any source.
/// Does nothing to healing :)
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(FragileSystem))]
public sealed partial class FragileComponent : Component
{
    /// <summary>
    /// Modifier applied to incoming damage.
    /// </summary>
    [DataField]
    public float Modifier = 20000f;
}
