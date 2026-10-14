// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Objectives.Components;
using Content.Shared.Objectives;

namespace Content.Trauma.Server.JobListings;

/// <summary>
/// Generates side jobs for stealing a grand theft item based on a list of steal target group prototypes.
/// One prototype is spawned per each theft item, then it's StealGroup field is set.
/// </summary>
[RegisterComponent]
public sealed partial class StealSideJobGeneratorComponent : Component
{
    /// <summary>
    /// The base side job prototype which is spawned and then modified for each area.
    /// </summary>
    [DataField]
    public EntProtoId<StealConditionComponent> Proto;

    /// <summary>
    /// The areas being bugged.
    /// </summary>
    [DataField]
    public List<ProtoId<StealTargetGroupPrototype>> Targets;
}
