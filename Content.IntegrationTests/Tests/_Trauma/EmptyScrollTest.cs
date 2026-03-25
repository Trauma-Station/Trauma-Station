// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.EmptyScroll;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure all empty scroll prayers work without throwing.
/// For the test to exit cleanly prayers also can't affect anything outside the current map.
/// </summary>
[TestFixture]
public sealed class EmptyScrollTest
{
    public static readonly EntProtoId Human = "MobHuman";

    [Test]
    public async Task PrayersTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var scroll = entMan.System<EmptyScrollSystem>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var prayer in scroll.AllPrayers.Values)
                {
                    // spawn fresh urist every time incase he gets gibbed or whatever
                    var urist = entMan.SpawnEntity(Human, map.GridCoords);
                    scroll.Pray(urist, prayer);
                    entMan.DeleteEntity(urist);
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
