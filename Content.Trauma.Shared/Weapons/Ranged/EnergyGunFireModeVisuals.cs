// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Weapons.Ranged;

[Serializable, NetSerializable]
public enum EnergyGunFireModeVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum EnergyGunFireModeState : byte
{
    Disabler,
    Lethal,
    Special,
    Cooling,
    Heating
}
