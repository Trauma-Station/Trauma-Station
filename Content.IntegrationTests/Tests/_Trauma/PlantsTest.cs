// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Utility;
using Content.Shared.Botany.Components;
using Robust.Client.GameObjects;
using System.Linq;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed partial class PlantsTest : GameTest
{
    /// <summary>
    /// Makes sure all plants have their growth stages in their RSIs.
    /// </summary>
    [Test]
    public async Task PlantSpritesTest()
    {
        var failed = new List<string>();
        var plantName = CEntMan.ComponentFactory.CompName<PlantComponent>();
        var spriteName = CEntMan.ComponentFactory.CompName<SpriteComponent>();
        foreach (var id in GameDataScrounger.EntitiesWithComponent(plantName))
        {
            var proto = CProtoMan.Index<EntityPrototype>(id);
            if (!proto.TryComp<SpriteComponent>(spriteName, out var sprite))
            {
                failed.Add($"{id} is missing SpriteComponent");
                continue;
            }

            if (!proto.TryComp<PlantComponent>(plantName, out var plant))
            {
                failed.Add("Scrounger is broken");
                continue;
            }

            if (sprite.BaseRSI is not { } rsi)
            {
                failed.Add($"{id} SpriteComponent is missing its sprite field!");
                continue;
            }

            var stages = plant.GrowthStages;
            for (var i = 1; i <= stages; i++)
            {
                var state = $"stage-{i}";
                if (!rsi.TryGetState(state, out _)) // W shitty dict wrapper api
                    failed.Add($"{id} sprite ({rsi.Path}) is missing {state}!");
            }
        }

        if (failed.Count > 0)
            Assert.Fail(string.Join("\n", failed));
    }
}
