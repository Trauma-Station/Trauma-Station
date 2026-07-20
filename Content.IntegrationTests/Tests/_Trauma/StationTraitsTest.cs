// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Trauma.Server.Station;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Trauma;

public sealed class StationTraitsTest : GameTest
{
    /// <summary>
    /// Makes sure all traits can be added to a station at the same time without throwing.
    /// </summary>
    [Test]
    public async Task AllTraitsTest()
    {
        var server = Pair.Server;
        var traits = SEntMan.System<StationTraitsSystem>();
        var ticker = SEntMan.System<GameTicker>();

        traits.ForceAllTraits();

        server.CfgMan.SetCVar(CCVars.GameMap, "Bagel"); // bagel has enough areas but not too big to bog down tests too much

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        ticker.ToggleReadyAll(true);
        await server.WaitPost(() => ticker.StartRound()); // start the round and make sure no errors at any point
        await RunTicksSync(30); // for anything funny
        await server.WaitPost(() => ticker.RestartRound()); // and make sure it all gets cleaned up with no errors
    }
}
