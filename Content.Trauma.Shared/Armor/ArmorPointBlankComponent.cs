// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Armor;

/// <summary>
/// Makes armor values get reduced by precise attacks below a certain distance.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ArmorPointBlankSystem))]
public sealed partial class ArmorPointBlankComponent : Component
{
    /// <summary>
    /// Range to check the damage origin against, disables this component if 0.
    /// </summary>
    [DataField]
    public float Range = 1.25f;

    /// <summary>
    /// Value that we multiply armor resistances by
    /// </summary>
    [DataField]
    public float ResistancePenalty = 0.5f;
}
