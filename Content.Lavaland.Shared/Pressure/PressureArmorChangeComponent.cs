// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Lavaland.Shared.Pressure;

/// <summary>
/// Increases ArmorPenetration for this armor entity while <see cref="PressureEfficiencyComponent"/> does not apply.
/// Used to make lavaland armor less protective on station.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedPressureEfficiencyChangeSystem))]
public sealed partial class PressureArmorChangeComponent : Component
{
    [DataField]
    public float ExtraPenetrationModifier = 0.5f;
}
