// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BloodCultistRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// The blood cult gamerule that created this cultist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Rule;
}
