// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Multihit;

/// <summary>
/// Allows weapon to trigger follow-up attacks with other weapons in user hands
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MultihitComponent : Component
{
    /// <summary>
    /// Damage multiplier on follow-up attack
    /// Damage gets reduced progressively on each follow up attack if this value is less than 1
    /// </summary>
    [DataField]
    public float DamageMultiplier = 0.67f;

    /// <summary>
    /// Max follow-up attacks, less or equal than 0 for no restriction
    /// </summary>
    [DataField]
    public int MaxFollowUpAttacks;

    /// <summary>
    /// Delay between follow-up attacks
    /// </summary>
    [DataField]
    public TimeSpan MultihitDelay = TimeSpan.FromSeconds(0.25);

    /// <summary>
    /// Increase this melee weapon attack delay by this amount for each follow-up attack
    /// </summary>
    [DataField]
    public TimeSpan DelayPenalty = TimeSpan.Zero;

    /// <summary>
    /// Whitelist for weapons to do follow-up attack with
    /// </summary>
    [DataField]
    public EntityWhitelist? MultihitWhitelist;

    /// <summary>
    /// Conditions for user
    /// </summary>
    [DataField]
    public EntityCondition[]? UserConditions;
}
