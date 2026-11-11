// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles.Components;

namespace Content.Trauma.Shared.Roles;

/// <summary>
/// Added to mind role entities to tag that they are a space ninja.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SamuraiRoleComponent : BaseMindRoleComponent;
