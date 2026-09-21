// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Strip;

/// <summary>
/// Allows interaction across containers for this user and a set of allowed entities.
/// Automatically removes allowed if a storage UI is closed on a target.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AccessibleOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Allowed = new();
}
