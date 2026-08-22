// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Weapons.Classes;

/// <summary>
/// Assigns a weapon to a certain <see cref="WeaponClassPrototype"/>.
/// Changes how effective guns and melee weapons are in combat depending on your training knowledge.
/// Weapons with no class are assumed to be trivial to use with nothing changed about them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(WeaponClassSystem))]
public sealed partial class WeaponClassComponent : Component
{
    /// <summary>
    /// The class of this weapon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeaponClassPrototype> Class;

    /// <summary>
    /// Whether the class is shown when examined.
    /// </summary>
    [DataField]
    public bool Examinable = true;
}
