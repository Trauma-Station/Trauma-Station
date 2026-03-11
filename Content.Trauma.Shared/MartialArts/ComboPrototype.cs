// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.MartialArts;

[Prototype]
public sealed partial class ComboPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    /// <summary>
    /// Events that should happen on user that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Events that should happen on the opponent that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? OpponentEffects;

    /// <summary>
    /// Events that should happen on both users that this move will do on perform?
    /// </summary>
    [DataField]
    public EntityEffect[]? CombinedEffects;

    /// <summary>
    /// Level required to perform?
    /// </summary>
    [DataField]
    public int LevelRequired = 0;

    /// <summary>
    /// Level required to perform?
    /// </summary>
    [DataField]
    public int LevelExceeded = -1;

    /// <summary>
    /// Should give experience?
    /// </summary>
    [DataField]
    public bool GiveExperience = true;
}

[Prototype]
public sealed partial class ComboListPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<ComboPrototype>> Combos = new();
}
