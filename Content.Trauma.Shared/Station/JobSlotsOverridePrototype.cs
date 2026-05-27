// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// Overrides job slots for the station if the round starts with a playercount in a defined range.
/// Only one override will be used, do not make multiple that have overlapping ranges.
/// If a range point is left as null, it will not be checked.
/// </summary>
[Prototype]
public sealed partial class JobSlotsOverridePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Minimum players to use this override for.
    /// </summary>
    [DataField]
    public int? Min;

    /// <summary>
    /// Maximum players to use this override for.
    /// </summary>
    [DataField]
    public int? Max;

    /// <summary>
    /// Each job slot and the count to set it to.
    /// -1 means unlimited.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, int> Jobs = new();
}
