// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Store;

/// <summary>
/// Component added to a store entity to enable side-jobs.
/// Used for progressive traitor.
/// </summary>

[RegisterComponent, NetworkedComponent]
public sealed partial class StoreJobListingsComponent : Component
{
    /// <summary>
    /// How many jobs are offered at once.
    /// </summary>
    [DataField]
    public int JobCount;
}
