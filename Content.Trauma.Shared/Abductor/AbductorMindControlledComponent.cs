// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Mindcontrol;

[RegisterComponent]
public sealed partial class AbductorMindcontrolledRoleComponent : BaseMindRoleComponent
{
    [DataField] public EntityUid? MasterUid;
}
