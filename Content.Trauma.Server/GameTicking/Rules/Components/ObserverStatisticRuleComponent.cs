// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles.Components;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component to track statistics on observers.
/// </summary>
[RegisterComponent, Access(typeof(ObserverStatisticRuleSystem))]
public sealed partial class ObserverStatisticRuleComponent : Component
{
    /// <summary>
    /// Character name for the entity with the most unique observers
    /// </summary>
    [DataField]
    public string MostPopularCharacterName = "noone";

    /// <summary>
    /// Username for the player controlling the entity with the most unique observers
    /// </summary>
    [DataField]
    public string MostPopularUserName = "";

    /// <summary>
    /// Number of unique followers for <see cref="MostPopularCharacterName"/>
    /// </summary>
    [DataField]
    public int MostPopularEntityPopularity;
}
