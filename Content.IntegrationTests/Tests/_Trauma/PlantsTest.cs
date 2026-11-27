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

        void Test(EntityUid uid, string id)
        {
            if (!CTryComp<SpriteComponent>(uid, out var sprite))
            {
                failed.Add($"{id} is missing SpriteComponent");
                return;
            }

            if (!CTryComp<PlantComponent>(uid, out var plant))
            {
                failed.Add("Scrounger is broken");
                return;
            }

            if (sprite.BaseRSI is not { } rsi)
            {
                failed.Add($"{id} SpriteComponent is missing its sprite field!");
                return;
            }

            var stages = plant.GrowthStages;
            for (var i = 1; i <= stages; i++)
            {
                var state = $"stage-{i}";
                if (!rsi.TryGetState(state, out _)) // W shitty dict wrapper api
                    failed.Add($"{id} sprite ({rsi.Path}) is missing {state}!");
            }
        }

        await Client.WaitPost(() =>
        {
            foreach (var id in GameDataScrounger.EntitiesWithComponent(plantName))
            {
                // have to spawn it because Sprite.BaseRSI only gets set by ComponentAdd handler
                var uid = CSpawn(id);
                Test(uid, id);
                CDel(uid);
            }
        });

        if (failed.Count > 0)
            Assert.Fail(string.Join("\n", failed));
    }
}
