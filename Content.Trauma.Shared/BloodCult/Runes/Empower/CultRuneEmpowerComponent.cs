// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Runes.Empower;

/// <summary>
/// Gives the user <c>BloodCultEmpoweredComponent</c> and allows to choose spells.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultRuneEmpowerComponent : Component
{
    /// <summary>
    /// Selected spell.
    /// </summary>
    [DataField]
    public EntProtoId Spell;
}
