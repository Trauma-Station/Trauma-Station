// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Areas;
using Content.Trauma.Shared.JobListings;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Generates side jobs for bugging an area based on a list of areas.
/// One prototype is spawned per each area, then it's TargetArea field is set.
/// </summary>
[RegisterComponent]
public sealed partial class BugSideJobGeneratorComponent : Component
{
    /// <summary>
    /// The base side job prototype which is spawned and then modified for each area.
    /// </summary>
    [DataField]
    public EntProtoId<BugAreaConditionComponent> Proto;

    /// <summary>
    /// The areas being bugged.
    /// </summary>
    [DataField]
    public List<EntProtoId<AreaComponent>> Areas;
}
