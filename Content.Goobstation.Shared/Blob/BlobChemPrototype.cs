// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Goobstation.Shared.Blob;

[Prototype]
public sealed partial class BlobChemPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Info = string.Empty;

    [DataField(required: true)]
    public Color Color;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField(required: true)]
    public Solution SmokeSolution = default!;

    [DataField]
    public int HealingScale = 1;

    /// <summary>
    /// Extra points added to resource blob production.
    /// </summary>
    [DataField]
    public int BonusPoints;

    [DataField]
    public EntityEffect[]? AttackEffects;

    [DataField]
    public EntityEffect[]? DestructionEffects;

    [DataField]
    public EntityEffect[]? PodDeathEffects;

    [DataField]
    public ProtoId<DamageModifierSetPrototype> DamageModifiers = "BaseBlob";

    [DataField]
    public float ExplosionResistance = 0f;
}
