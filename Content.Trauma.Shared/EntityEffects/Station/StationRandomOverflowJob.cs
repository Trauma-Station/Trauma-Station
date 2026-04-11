// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Roles;

namespace Content.Trauma.Shared.EntityEffects.Station;

/// <summary>
/// Station effect that makes a random job slot unlimited.
/// </summary>
public sealed partial class StationRandomOverflowJob : EntityEffectBase<StationRandomOverflowJob>
{
    /// <summary>
    /// Never try to make these jobs unlimited.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> IgnoredJobs = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager proto, IEntitySystemManager entSys)
        => null;
}
