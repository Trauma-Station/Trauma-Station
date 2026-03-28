// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.CosmicCult;

/// <summary>
///     Associates an entity with a specific cosmic cult gamerule
/// </summary>
[RegisterComponent]
public sealed partial class CosmicCultAssociatedRuleComponent : Component
{
    /// <summary>
    ///     The gamerule that this entity is associated with
    /// </summary>
    [DataField]
    public EntityUid CultGamerule;
}
