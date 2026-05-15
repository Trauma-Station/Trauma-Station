// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Shared.MartialArts;

[Prototype]
public sealed partial class ComboPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    /// <summary>
    /// Effects applied to the user when performed.
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Effects applied to the opponent when performed.
    /// </summary>
    [DataField]
    public EntityEffect[]? OpponentEffects;

    /// <summary>
    /// Effects applied to both the user and opponent when performed.
    /// </summary>
    [DataField]
    public EntityEffect[]? CombinedEffects;

    /// <summary>
    /// Conditions to check against the user when trying to perform this combo.
    /// </summary>
    [DataField]
    public EntityCondition[]? UserConditions;

    /// <summary>
    /// Conditions to check against the target when trying to perform this combo.
    /// </summary>
    [DataField]
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
