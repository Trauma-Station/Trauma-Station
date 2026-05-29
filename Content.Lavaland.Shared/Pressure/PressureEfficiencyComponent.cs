// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;

namespace Content.Lavaland.Shared.Pressure;

/// <summary>
/// Data for modifying efficiency of some process based on the containing atmosphere's pressure.
/// <seealso cref="PressureDamageChangeComponent"/>
/// <seealso cref="PressureArmorChangeComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PressureEfficiencyComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    public float LowerBound = Atmospherics.OneAtmosphere * 0.2f;

    [DataField, AutoNetworkedField]
    public float UpperBound = Atmospherics.OneAtmosphere * 0.5f;

    [DataField, AutoNetworkedField]
    public bool ApplyWhenInRange = true;
}
