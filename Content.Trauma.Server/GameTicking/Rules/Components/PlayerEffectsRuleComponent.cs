// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Roles;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component to run entity effects on all players that spawn/join, optionally with a valid job.
/// </summary>
[RegisterComponent, Access(typeof(PlayerEffectsRuleSystem))]
public sealed partial class PlayerEffectsRuleComponent : Component
{
    /// <summary>
    /// If non-null, only run effects if the player has one of these jobs.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? Jobs;

    /// <summary>
    /// The effects to run on the player's attached entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;
}
