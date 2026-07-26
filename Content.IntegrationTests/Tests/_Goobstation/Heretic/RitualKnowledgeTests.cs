// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.Tests._Goobstation.Heretic;

[TestFixture, TestOf(typeof(Trauma.Shared.Heretic.Components.Side.HereticKnowledgeRitualComponent))]
public sealed partial class RitualKnowledgeTests : GameTest
{
    [SidedDependency(Side.Server)] private EntityWhitelistSystem _whitelist = default!;

    [Test]
    public async Task ValidateTagsHaveItems()
    {
        await Server.WaitAssertion(() =>
        {
            var ingredients = SProtoMan.EnumeratePrototypes<RitualIngredientDatasetPrototype>()
                .SelectMany(x => x.Ingredients)
                .ToHashSet();

            foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                ingredients.RemoveWhere(x => _whitelist.CheckBoth(proto, x.Blacklist, x.Whitelist));
            }

            Assert.That(ingredients,
                Is.Empty,
                $"The following ritual ingredient tags are not provided by any available entities: {string.Join(", ", ingredients.Select(x => x.Name))}");
        });
    }
}
