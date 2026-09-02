// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.StationEvents;

/// <summary>
/// Limits the number of rules a scheduler can run.
/// Ends it once the limit has been reached.
/// </summary>
[RegisterComponent, Access(typeof(RuleSchedulerLimitSystem))]
public sealed partial class RuleSchedulerLimitComponent : Component
{
    /// <summary>
    /// How many rules can be spawned.
    /// </summary>
    [DataField(required: true)]
    public int Limit;

    /// <summary>
    /// How many rules were spawned so far.
    /// </summary>
    [DataField]
    public int Count;
}
