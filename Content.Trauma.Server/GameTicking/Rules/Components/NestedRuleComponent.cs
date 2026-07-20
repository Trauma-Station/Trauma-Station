// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Trauma.Server.GameTicking.Rules.Components;

/// <summary>
/// Starts every nested gamerule an entity table picks.
/// </summary>
[RegisterComponent, Access(typeof(NestedRuleSystem))]
public sealed partial class NestedRuleComponent : Component
{
    /// <summary>
    /// The gamerules to start.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Rules = default!;
}
