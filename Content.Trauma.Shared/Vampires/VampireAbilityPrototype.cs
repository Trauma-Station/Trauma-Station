// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.MobClass;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Trauma.Shared.Vampires;

/// <summary>
/// Prototype that holds data about vampire abilities.
/// </summary>
[Prototype]
public sealed partial class VampireAbilityPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<VampireAbilityPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Extra conditions the vampire must meet to unlock this ability.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// The class this ability belongs to. If null, all vampires can get it.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ProtoId<MobClassPrototype>? Class;

    /// <summary>
    /// We must not be this class to unlock this ability.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ProtoId<MobClassPrototype>? BlacklistClass;


    /// <summary>
    /// How much <see cref="VampireComponent.TotalBlood"/> this ability requires.
    /// </summary>
    [DataField]
    public int Cost;

    /// <summary>
    /// Effects to run when unlocking this ability.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] OnUnlock = default!;
}
