using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpyRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public string Briefing = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? OwnedUplink;

    [DataField, AutoNetworkedField]
    public EntityUid? Rule;

    // Either SpyRewardPrototype or ListingPrototype
    [DataField, AutoNetworkedField]
    public List<string> AvailableRewards = new();

    // Used for roundend manifest
    [DataField]
    public int ClaimedBounties;

    [DataField]
    public TimeSpan MakeUplinkTime = TimeSpan.FromSeconds(10);
}
