using System.Collections.Generic;
using System.Numerics;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Station.Systems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class KnowledgeTest
{
    /// <summary>
    /// Makes sure that engineers can build an APC.
    /// </summary>
    /// <returns></returns>
    [Test]
    public async Task TestEngineerCanBuildAPC()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var mapMan = server.MapMan;
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;

        var stationSystem = entMan.System<StationJobsSystem>();
        var mindSystem = entMan.System<SharedMindSystem>();
        var consSystem = entMan.System<ConstructionSystem>();

        await server.WaitPost(() =>
        {
            var mapId = pair.CreateTestMap();
            var coords = pair.TestMap.GridCoords;
            var grid = pair.TestMap.Grid;
            var engineer = entMan.SpawnEntity("MobHuman", coords);

            var jobProto = new ProtoId<JobPrototype>("StationEngineer");

            stationSystem.MakeJobUnlimited(grid, jobProto);

            var apcFrame = entMan.SpawnEntity("APCFrame", coords);
            var construction = entMan.GetComponent<ConstructionComponent>(apcFrame);

            Assert.That(entMan.HasComponent<KnowledgeHolderComponent>(engineer), "Engineer must have a KnowledgeHolder");

            var consSystem = entMan.System<ConstructionSystem>();

            var graph = protoMan.Index<ConstructionGraphPrototype>(construction.Graph);
            var startNode = graph.Nodes[construction.Node];

            Assert.That(startNode.Edges.Count, Is.GreaterThan(0), "APC Frame should have outgoing edges");

            foreach (var edge in startNode.Edges)
            {
                // This is the core check: Can the engineer satisfy the conditions?
                // Your KnowledgeCondition should return true here.
                var canDo = consSystem.CheckConditions(apcFrame, edge.Conditions);
                Assert.That(canDo, $"Engineer should be able to perform edge to {edge.Target}");
            }
        });

        await pair.CleanReturnAsync();
    }
}
