// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.ItemToggle;

/// <summary>
/// Passively drains the entity's battery while it is toggled on.
/// Requires <see cref="ItemToggleComponent"/> and <see cref="BatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleBatteryDrainComponent : Component
{
    /// <summary>
    /// Passive power draw in watts while the item is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DrawRate = 5f;
}
