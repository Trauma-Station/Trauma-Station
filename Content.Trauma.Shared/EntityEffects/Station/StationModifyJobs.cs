// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that modifies its job slots.
/// </summary>
public sealed partial class StationModifyJobs : EntityEffectBase<StationModifyJobs>
{
    /// <summary>
    /// How many job slots to add for each job.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, int> Add = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
