// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Weapons.EnergyKatanaSheath;

/// <summary>
/// Binds sheathed katana to ninja on wear
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EnergyKatanaSheathComponent : Component
{
    [DataField]
    public string Slot = "item";
}
