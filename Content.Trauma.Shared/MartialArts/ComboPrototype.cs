// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Trauma.Shared.MartialArts;

[Prototype]
public sealed partial class ComboPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ComboPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    /// <summary>
    /// Effects applied to the user when performed.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Effects applied to the opponent when performed.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityEffect[]? OpponentEffects;

    /// <summary>
    /// Effects applied to both the user and opponent when performed.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityEffect[]? CombinedEffects;

    /// <summary>
    /// Conditions to check against the user when trying to perform this combo.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityCondition[]? UserConditions;

    /// <summary>
    /// Conditions to check against the target when trying to perform this combo.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// Level required to perform?
    /// </summary>
    [DataField]
    public int LevelRequired = 0;

    /// <summary>
    /// Level at which this move can't be done anymore, for shitty move upgrade logic.
    /// Ignored when this is negative.
    /// </summary>
    [DataField]
    public int LevelExceeded = -1;

    /// <summary>
    /// Whether to give experience with the martial art when performed.
    /// </summary>
    [DataField]
    public bool GiveExperience = true;
}
