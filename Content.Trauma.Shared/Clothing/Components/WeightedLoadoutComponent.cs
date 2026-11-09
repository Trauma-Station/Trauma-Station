// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Content.Trauma.Shared.Clothing.Prototypes;

namespace Content.Trauma.Shared.Clothing.Components;

/// <summary>
/// Component that initializes the entity with a random loadout.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeightedLoadoutComponent : Component
{
    /// <summary>
    /// Weighted dictionary of premade loadouts
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StartingGearPrototype>, float> Weights = new();

    /// <summary>
    /// Optional randomized loadout, one archetype is picked and its slots are filled.
    /// </summary>
    [DataField]
    public ProtoId<RandomLoadoutPrototype>? Loadout;
}
