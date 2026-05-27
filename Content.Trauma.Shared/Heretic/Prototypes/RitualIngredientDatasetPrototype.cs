// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Rituals;

namespace Content.Trauma.Shared.Heretic.Prototypes;

[Prototype]
public sealed partial class RitualIngredientDatasetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public RitualIngredient[] Ingredients = default!;
}
