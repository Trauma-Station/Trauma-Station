// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.Tests._Goobstation.Heretic;

[TestFixture, TestOf(typeof(RitualIngredientDatasetPrototype)]
public sealed partial class RitualKnowledgeTests : GameTest
{
    [SidedDependency(Side.Server)] private EntityWhitelistSystem _whitelist = default!;

    [Test]
    public async Task ValidateTagsHaveItems()
    {
        var ingredients = SProtoMan.EnumeratePrototypes<RitualIngredientDatasetPrototype>()
            .SelectMany(x => x.Ingredients)
            .ToHashSet();

        // TODO: this can still contain completely unobtainable entities
        foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            ingredients.RemoveWhere(x => _whitelist.CheckBoth(proto, x.Blacklist, x.Whitelist));
            if (ingredients.Count == 0)
                return;
        }

        Assert.That(ingredients,
            Is.Empty,
            $"The following ritual ingredient tags are not provided by any available entities: {string.Join(", ", ingredients.Select(x => x.Name))}");
    }
}
