// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Quality;

[Prototype]
public sealed partial class QualityPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Gun = 0.9f;

    [DataField]
    public float Armor = 0.95f;

    [DataField]
    public float ClothingDelay = 0.87f;

    [DataField]
    public float ExplosionResist = 0.87f;

    [DataField]
    public float StaminaResist = 0.87f;

    /// <summary>
    /// Used for destructible scale e.g. for walls
    /// Default is 250% health at +5 quality, 40% at -5
    /// </summary>
    [DataField]
    public float Health = 1.201f;

    [DataField]
    public float SelfDamage = 0.87f;

    [DataField]
    public float Damage = 1.125f;

    [DataField]
    public float Projectile = 1.125f;

    [DataField]
    public float Durability = 1.12f;

    /// <summary>
    /// Used for damage done to shields when protecting you, should be < 1
    /// </summary>
    [DataField]
    public float Shield = 0.89f;

    /// <summary>
    /// Used for how much damage shields protect you from, should be > 1
    /// </summary>
    [DataField]
    public float ShieldFlat = 1.12f;

    [DataField]
    public float MeleeDamage = 1.05f;

    [DataField]
    public float Price = 1.5f;
}
