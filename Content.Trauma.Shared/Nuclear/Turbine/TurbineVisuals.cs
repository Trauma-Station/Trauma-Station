// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Turbine;

/// <summary>
/// Appearance keys for the turbine.
/// </summary>
[Serializable, NetSerializable]
public enum TurbineVisuals : byte
{
    TurbineRuined,
    DamageSpark,
    DamageSmoke,
    TurbineSpeed,
}

/// <summary>
/// Visual sprite layers for the turbine.
/// </summary>
[Serializable, NetSerializable]
public enum TurbineVisualLayers : byte
{
    TurbineRuined,
    DamageSpark,
    DamageSmoke,
    TurbineSpeed,
}
