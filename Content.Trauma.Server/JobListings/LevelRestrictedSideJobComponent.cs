// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityTable;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Cancels generating a sidejob if the traitor doesn't have the specific 'effective reputation level'.
/// Effective reputation level means that the sidejob generator will pretend the traitor has a lower reputation if no objectives for the current reputation can be found.
/// </summary>
[RegisterComponent]
public sealed partial class LevelRestrictedSideJobComponent : Component
{
    /// <summary>
    /// The minimum effective reputation level the traitor must have.
    /// </summary>
    [DataField]
    public int Level;
}
