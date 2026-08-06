// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Trauma.Shared.EmptyScroll;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure all empty scroll prayers work without throwing.
/// For the test to exit cleanly prayers also can't affect anything outside the current map.
/// </summary>
public sealed class EmptyScrollTest : GameTest
{
    public static readonly EntProtoId Human = "MobHuman";

    [SidedDependency(Side.Server)] private EmptyScrollSystem _scroll = default!;

    [Test]
    public async Task PrayersTest()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var prayer in _scroll.AllPrayers.Values)
                {
                    // spawn fresh urist every time incase he gets gibbed or whatever
                    var urist = SEntMan.SpawnEntity(Human, map.GridCoords);
                    _scroll.Pray(urist, prayer);
                    SEntMan.DeleteEntity(urist);
                }
            });
        });
    }
}
