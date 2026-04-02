// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Stores forging data for a type of metal.
/// Used for setting stats of procedurally generated forged items.
/// </summary>
[Prototype]
public sealed partial class MetalPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Material name prefix to give to procedurally generated items.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// Color modulation for forged item sprites.
    /// </summary>
    [DataField(required: true)]
    public Color Color;

    /// <summary>
    /// Sprite state for procedurally generated items.
    /// </summary>
    [DataField(required: true)]
    public string IngotSprite = string.Empty;

    /// <summary>
    /// Density in kg/L or g/cm^3
    /// </summary>
    [DataField(required: true)]
    public float Density;

    /// <summary>
    /// Ideal working temperature in Kelvin.
    /// </summary>
    [DataField(required: true)]
    public float WorkingTemp;

    /// <summary>
    /// Melting point in Kelvin.
    /// </summary>
    [DataField(required: true)]
    public float MeltTemp;

    /// <summary>
    /// Temperatue range added and subtracted from <see cref="WorkingTemp"/> for being too cold or hot to work on.
    /// </summary>
    [DataField(required: true)]
    public float WorkingRange;

    [ViewVariables]
    public float MinTemp => WorkingTemp - WorkingRange;

    [ViewVariables]
    public float MaxTemp => WorkingTemp + WorkingRange;

    /// <summary>
    /// Modifier for how much damage a <c>Workable</c> piece takes to be wrought into shape.
    /// </summary>
    [DataField]
    public FixedPoint2 WorkScale = 1;

    /// <summary>
    /// Modifier for how much durability items with <c>Durability</c> have when made of this metal.
    /// </summary>
    [DataField]
    public FixedPoint2 Durability = 1;

    /// <summary>
    /// Modifier for melee weapon speed.
    /// </summary>
    [DataField]
    public float Speed = 1f;

    /// <summary>
    /// Modifiers for damage done by melee weapons and thrown items.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> Damage = new();

    /// <summary>
    /// Overheated brittle item to spawn when overheating it too much.
    /// Breaks into scraps which can be remelted in a bloomery.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Overheated;

    /// <summary>
    /// Offset applied to required practical skills for quality rolling.
    /// Basically how hard this metal is to work with.
    /// </summary>
    [DataField]
    public int MasteryOffset;
}
