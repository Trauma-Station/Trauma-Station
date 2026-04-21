// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Lavaland.Shared.Megafauna.Components;
using Content.Lavaland.Shared.Megafauna.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Lavaland.Megafauna;

[TestFixture]
[TestOf(typeof(MegafaunaAiComponent))]
[TestOf(typeof(MegafaunaSystem))]
public sealed class MegafaunaTest : GameTest
{
    public const string TestBoss = "MobHierophant";

    [Test]
    public async Task LaunchAndShutdownMegafauna()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var entSysMan = server.ResolveDependency<IEntitySystemManager>();

        EntityUid bossEntity = default;
        MegafaunaAiComponent megafaunaAi = null;
        MegafaunaSystem megafaunaSystem = null;

        await server.WaitPost(() =>
        {
            bossEntity = entMan.SpawnAtPosition(TestBoss, testMap.GridCoords);
            megafaunaAi = entMan.GetComponent<MegafaunaAiComponent>(bossEntity);
            megafaunaSystem = entSysMan.GetEntitySystem<MegafaunaSystem>();
        });

        await server.WaitRunTicks(5);

        // Check that boss is clear
        Assert.That(megafaunaAi.Active, Is.False);
        Assert.That(megafaunaAi.Schedule, Is.Empty);

        await server.WaitAssertion(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                megafaunaSystem.StartupMegafauna((bossEntity, megafaunaAi));
            });
        });

        await server.WaitRunTicks(1);

        // Should start up now
        Assert.That(megafaunaAi.Active, Is.True);
        Assert.That(megafaunaAi.Schedule, Is.Not.Empty);

        await server.WaitAssertion(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                megafaunaSystem.ShutdownMegafauna((bossEntity, megafaunaAi));
            });
        });

        await server.WaitRunTicks(1);

        // Should be clear again
        Assert.That(megafaunaAi.Active, Is.False);
        Assert.That(megafaunaAi.Schedule, Is.Empty);
    }

    /*[Test]
    public async Task TestMegafaunaAi()
    {
        var pair = Pair;
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var entSysMan = server.ResolveDependency<IEntitySystemManager>();

        EntityUid bossEntity = default;
        MegafaunaAiComponent megafaunaAi = null;
        MegafaunaSystem megafaunaSystem = null;

        await server.WaitPost(() =>
        {
            bossEntity = entMan.SpawnAtPosition(TestBoss, testMap.GridCoords);
            megafaunaAi = entMan.GetComponent<MegafaunaAiComponent>(bossEntity);
            megafaunaSystem = entSysMan.GetEntitySystem<MegafaunaSystem>();
        });

        await server.WaitRunTicks(5);
    }*/
}
