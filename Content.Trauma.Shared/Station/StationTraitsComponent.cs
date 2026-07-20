// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Station;

/// <summary>
/// Station component to pick some random station traits before starting the round.
/// They are shown in the station report.
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

    /// <summary>
    /// All picked traits which are active.
    /// </summary>
    [DataField]
    public List<ProtoId<StationTraitPrototype>> Picked = new();

    /// <summary>
    /// All picked traits which are shown in the station report.
    /// </summary>
    [DataField]
    public List<ProtoId<StationTraitPrototype>> Reported = new();

    /// <summary>
    /// Set to true to prevent running trait StartEffects multiple times.
    /// </summary>
    [DataField]
    public bool RanStartEffects;

    /// <summary>
    /// Set to true to prevent running trait MapEffects multiple times.
    /// </summary>
    [DataField]
    public bool RanMapEffects;
}
