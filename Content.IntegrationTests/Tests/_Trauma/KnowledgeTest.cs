using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Coordinates;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class EngineeringKnowledgeTest
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

        EntityUid engineer = default;
        EntityCoordinates coords = default;

        await server.WaitPost(() =>
        {
            var mapMan = server.ResolveDependency<IMapManager>();
            var entMan = server.ResolveDependency<IEntityManager>();
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            // 1. Setup Map and Engineer
            var mapId = mapMan.CreateMap();
            var grid = mapMan.CreateGrid(mapId);
            coords = new EntityCoordinates(grid.Owner, Vector2.Zero);
            engineer = entMan.SpawnEntity("MobHuman", coords);

            // Define our custom knowledge for this specific test case
            var grant = entMan.AddComponent<KnowledgeGrantComponent>(engineer);
            grant.Skills = new Dictionary<EntProtoId, int>
            {
                { "HandmadeKnowledge", 50 } ,
                { "TechnologyKnowledge", 50 } ,
                { "MaterialsKnowledge", 50 } ,
                { "DoorsKnowledge", 50 } ,
                { "AirlocksKnowledge", 50 } ,
                { "FurnitureKnowledge", 50 } ,
                { "InfrastructureKnowledge", 50 } ,
                { "ElectronicsKnowledge", 50 } ,
                { "WallsKnowledge", 50 } ,
                { "WindowsKnowledge", 50 } ,
                { "SmokeablesKnowledge", 50 } ,
            };
            entMan.InitializeAndStartEntity(engineer);
        });

        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var entMan = server.ResolveDependency<IEntityManager>();
            var protoMan = server.ResolveDependency<IPrototypeManager>();

            // 2. Setup the Construction Target (e.g., an APC frame)
            var apcFrame = entMan.SpawnEntity("APCFrame", coords);
            var construction = entMan.GetComponent<ConstructionComponent>(apcFrame);

            // 3. Verify the Engineer has the required knowledge components
            // This assumes your system adds knowledge to the Engineer prototype
            Assert.That(entMan.HasComponent<KnowledgeHolderComponent>(engineer), "Engineer must have a KnowledgeHolder");

            // 4. Simulate the interaction logic
            // We check if the current construction edge is valid for this user
            var consSystem = entMan.System<ConstructionSystem>();

            // Check the first step of the APC graph
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
