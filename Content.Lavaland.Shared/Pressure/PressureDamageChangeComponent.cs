// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Pressure;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PressureDamageChangeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float AppliedModifier = 2f; // Becomes 2 times better when in lavaland pressure environment

    [DataField]
    public bool ApplyToMelee = true;

    [DataField]
    public bool ApplyToProjectiles = true;
}
