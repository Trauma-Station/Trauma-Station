// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Carrying;

[RegisterComponent, NetworkedComponent, Access(typeof(CarryingSlowdownSystem))]
[AutoGenerateComponentState]
public sealed partial class CarryingSlowdownComponent : Component
{
    /// <summary>
    /// Modifier for both walk and sprint speed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Modifier = 1.0f;
}
