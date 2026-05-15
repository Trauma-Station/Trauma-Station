// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Prototypes;
using Content.Trauma.Shared.Heretic.Rituals;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticKnowledgeRitualComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<RitualIngredientDatasetPrototype>, int> Datasets;

    [DataField, AutoNetworkedField]
    public List<RitualIngredient> Ingredients = new();
}
