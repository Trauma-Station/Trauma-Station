// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.ShipGuns;

/// <summary>
/// Component for categorizing ship guns by class
/// </summary>
[RegisterComponent]
public sealed partial class ShipGunClassComponent : Component
{
    /// <summary>
    /// The class of ship gun
    /// </summary>
    [DataField("shipClass")]
    public ShipGunClass Class = ShipGunClass.Medium;
}

/// <summary>
/// Classes of ship guns
/// </summary>
[Serializable, NetSerializable]
public enum ShipGunClass
{
    Light,
    Medium,
    Heavy
}
