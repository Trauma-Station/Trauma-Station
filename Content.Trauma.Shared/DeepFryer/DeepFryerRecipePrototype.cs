// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;

namespace Content.Trauma.Shared.DeepFryer;

/// <summary>
/// A deep fryer recipe that requires at least 1 item, and optionally consumes some reagents.
/// The resulting food will be deep fried automatically.
/// </summary>
[Prototype]
public sealed partial class DeepFryerRecipePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId Result;

    /// <summary>
    /// Tag prototype for an item ingredient and how many of it you need.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<TagPrototype>, int> Items = new();

    /// <summary>
    /// Reagents that must be present, ignored if this is empty.
    /// </summary>
    [DataField]
    public List<ReagentCost> Reagents = new();
}

/// <summary>
/// A cost for a recipe, <see cref="Quantity"/> u of any reagent from <see cref="Allowed"/> is needed for the recipe to be made.
/// Multiple smaller fractions of each reagent can contribute if you are impoverished.
/// </summary>
[DataRecord]
public partial record struct ReagentCost(HashSet<ProtoId<ReagentPrototype>> Allowed, FixedPoint2 Quantity);
