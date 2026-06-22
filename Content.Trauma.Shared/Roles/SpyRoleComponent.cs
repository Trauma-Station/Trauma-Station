using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

[RegisterComponent]
public sealed partial class SpyRoleComponent : BaseMindRoleComponent
{
    [DataField]
    public string Briefing = string.Empty;

    [DataField]
    public EntityUid? Rule;
}
