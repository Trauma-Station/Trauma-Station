// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Weapons.Melee;

/// <summary>
/// Components that runs effects on melee hit.
/// #KillAllTriggers
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectsOnMeleeHitComponent : Component
{
    /// <summary>
    /// Effects to run on the user, if not null.
    /// </summary>
    [DataField]
    public EntityEffect[]? UserEffects;

    /// <summary>
    /// Effects to run on the target, if not null.
    /// </summary>
    [DataField]
    public EntityEffect[]? TargetEffects;

    /// <summary>
    /// If true, it will run effects on every hit.
    /// </summary>
    [DataField]
    public bool EffectForEveryHit;

    /// <summary>
    /// Conditions that are checked on the target, before the entity effects.
    /// Useful if you want to run <see cref="UserEffects"/> based on target conditions.
    /// </summary>
    [DataField]
    public EntityCondition[]? TargetConditions;
}
