// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// Runs a list of entity effects on any player that spawns which passes some conditions.
/// This probably includes borgs AI etc.
/// </summary>
[Prototype]
public sealed partial class PlayerEffectsPrototype: IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// All of these conditions must pass for the spawned player.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    /// <summary>
    /// If all conditions pass these effects get applied to the spawned player.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
