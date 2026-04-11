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

    [DataField]
    public float Health = 0.8f;

    [DataField]
    public float SelfDamage = 0.87f;

    [DataField]
    public float Damage = 1.125f;

    [DataField]
    public float Projectile = 1.125f;

    [DataField]
    public float Durability = 1.12f;

    [DataField]
    public float Shield = 1.12f;

    [DataField]
    public float ShieldFlat = 1.12f;

    [DataField]
    public float MeleeDamage = 1.05f;

    [DataField]
    public float Price = 1.5f;
}
