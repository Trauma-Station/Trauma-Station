// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random;

namespace Content.Trauma.Shared.Clothing.Prototypes;

/// <summary>
/// Prototype that holds a weighted dictionary containing a role's possible loadouts.
/// </summary>
[Prototype]
public sealed partial class RandomLoadoutPrototype : IWeightedRandomPrototype<LoadoutArchetypePrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Loadouts per weight.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<LoadoutArchetypePrototype>, float> Weights { get; private set; } = new();
}
