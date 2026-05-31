// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// Component for use in actions, to open the mob class selector ui for the user.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActionMobClassComponent : Component
{
    /// <summary>
    /// Whether to remove the action after we choose a class.
    /// </summary>
    [DataField]
    public bool RemoveOnSelected = true;
}
