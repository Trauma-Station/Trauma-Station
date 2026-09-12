// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Generates side jobs via a list of prototype ids, with no extra initialising.
/// Side jobs are duplicates if they share prototype ids.
/// </summary>
[RegisterComponent]
public sealed partial class BasicSideJobGeneratorComponent : Component
{
    /// <summary>
    /// Side jobs this generator will generate.
    /// </summary>
    [DataField]
    public List<EntProtoId<SideJobComponent>> Protos;
}
