// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Gamerule;

/// <summary>
/// Component given to a blood cult's sacrifice target.
/// Used to reroll the target if they get deleted.
/// </summary>
[RegisterComponent]
public sealed partial class BloodCultTargetComponent : Component
{
    [DataField]
    public EntityUid Rule;
}
