// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Component that assigns an area to a specific department.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(AreaSystem))]
public sealed partial class DepartmentAreaComponent : Component
{
    [DataField(required: true)]
    public ProtoId<DepartmentPrototype> Department;
}
