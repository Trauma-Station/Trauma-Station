// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;

namespace Content.Trauma.Shared.Clothing.Components;

/// <summary>
/// Like Loadout, but each starting gear has a weight
/// One is picked at random and equipped on map init
/// </summary>
[RegisterComponent]
public sealed partial class WeightedLoadoutComponent : Component
{
    /// <summary>
    /// Premade loadouts, one is picked by weight
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StartingGearPrototype>, float> Weights = new();

    /// <summary>
    /// Optional randomized loadout, one archetype is picked and its slots are rolled
    /// Runs after <see cref="Weights"/>, only filling slots that are still empty
    /// </summary>
    [DataField]
    public ProtoId<RandomLoadoutPrototype>? Loadout;
}
