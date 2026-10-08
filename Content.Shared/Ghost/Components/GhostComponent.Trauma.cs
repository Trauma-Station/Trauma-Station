// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Ghost.Components;

public sealed partial class GhostComponent
{
    [DataField, AutoNetworkedField]
    public bool CanTakeGhostRoles = true;
}
