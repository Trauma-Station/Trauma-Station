// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Station;

/// <summary>
/// Station component to pick some random station traits before starting the round.
/// It will then send a report to all communications consoles with a summary.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationTraitsComponent : Component
{
    /// <summary>
    /// How many times to try roll a trait for each group.
    /// </summary>
    [DataField(required: true)]
    public int Rolls;

    /// <summary>
    /// Each trait group and the chance for each roll to pick a trait to succeed.
    /// A chance of 1.0 means it will always pick <see cref="Rolls"/> traits from the group.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<StationTraitGroup, float> Groups = new();
}
