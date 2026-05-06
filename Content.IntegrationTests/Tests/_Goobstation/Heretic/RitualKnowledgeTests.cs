// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Prototypes;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation.Heretic;

[TestFixture, TestOf(typeof(Trauma.Shared.Heretic.Components.Side.HereticKnowledgeRitualComponent))]
public sealed class RitualKnowledgeTests : GameTest
{
    [Test]
    public async Task ValidateTagsHaveItems()
    {
        var pair = Pair;
        var server = pair.Server;

        var tagSys = server.System<TagSystem>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var compFactory = server.EntMan.ComponentFactory;

        await server.WaitAssertion(() =>
        {
            var ingredients = protoMan.EnumeratePrototypes<RitualIngredientDatasetPrototype>()
                .SelectMany(x => x.Ingredients)
                .ToHashSet();

            foreach (var entProto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                ingredients.RemoveWhere(x => CheckBoth(entProto, x.Blacklist, x.Whitelist));
            }

            Assert.That(ingredients,
                Is.Empty,
                $"The following ritual ingredients (names) are not used by any available entities {string.Join(", ", ingredients.Select(x => x.Name))}");
        });

        return;

        bool CheckBoth(EntityPrototype proto, EntityWhitelist blacklist = null, EntityWhitelist whitelist = null)
        {
            return (blacklist == null || !IsValid(blacklist, proto)) && (whitelist == null || IsValid(whitelist, proto));
        }

        bool IsValid(EntityWhitelist list, EntityPrototype prototype)
        {
            if (list.Components is { } comps)
            {
                foreach (var name in comps)
                {
                    var comp = compFactory.GetRegistration(name).Type;
                    if (prototype.HasComponent(comp, compFactory))
                    {
                        if (!list.RequireAll)
                            return true;
                    }
                    else if (list.RequireAll)
                        return false;
                }
            }

            if (list.Tags is { } tags)
            {
                if (!prototype.TryGetComponent(out TagComponent tagComp, compFactory))
                    return false;

                return list.RequireAll ? tagSys.HasAllTags(tagComp, tags) : tagSys.HasAnyTag(tagComp, tags);
            }

            return list.RequireAll;
        }
    }
}
