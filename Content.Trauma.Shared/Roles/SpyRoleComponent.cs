using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

[RegisterComponent]
public sealed partial class SpyRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public string Briefing = string.Empty;

    [DataField]
    public EntityUid? Rule;

    // Either SpyRewardPrototype or ListingPrototype
    [DataField]
    public List<string> AvailableRewards = new();

    // Used for roundend manifest
    [DataField]
    public int ClaimedBounties;
}
