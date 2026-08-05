// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

/// <summary>
/// Mind role comp for spies
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpyRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// Briefing for character menu
    /// </summary>
    [DataField]
    public string Briefing = string.Empty;

    /// <summary>
    /// Our uplink entity, used for examine info
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OwnedUplink;

    /// <summary>
    /// Spy gamerule
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Rule;

    /// <summary>
    /// Rewards that are available for collection
    /// Either SpyRewardPrototype or ListingPrototype
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> AvailableRewards = new();

    /// <summary>
    /// How many bounties did we claim
    /// Used for roundend manifest
    /// </summary>
    [DataField]
    public int ClaimedBounties;

    /// <summary>
    /// Time to create new uplink in new pda
    /// </summary>
    [DataField]
    public TimeSpan MakeUplinkTime = TimeSpan.FromSeconds(10);
}
