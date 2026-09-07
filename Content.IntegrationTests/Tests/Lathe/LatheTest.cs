using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Lathe;
using Content.Shared.Materials;
//using Content.Shared.Prototypes; // Trauma - die
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

[TestFixture]
public sealed class LatheTest : GameTest
{
    [RunOnSide(Side.Server)] // Trauma
    [Test]
    public async Task TestLatheRecipeIngredientsFitLathe()
    {
        var pair = Pair;
        var server = pair.Server;

        /* Trauma - don't need this anymore + it would deadlock from being a server sided test
        var mapData = await pair.CreateTestMap();
        */

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var materialStorageSystem = server.System<SharedMaterialStorageSystem>();
        var whitelistSystem = server.System<EntityWhitelistSystem>();
        var latheSystem = server.System<SharedLatheSystem>();

        /* Trauma - no reason to tick the game at all
        await server.WaitAssertion(() =>
        {
        */
            // Find all the lathes
            // <Trauma> - microptimisation, remove linq jesus christ. also get the physical comp from materials immediately not for EVERY FUCKING LATHE
            var latheName = compFactory.CompName<LatheComponent>();
            var materialName = compFactory.CompName<PhysicalCompositionComponent>();
            var storageName = compFactory.CompName<MaterialStorageComponent>();
            var emagName = compFactory.CompName<EmagLatheRecipesComponent>();
            var latheProtos = new List<EntityPrototype>();
            var materialEntityProtos = new List<(EntityPrototype, PhysicalCompositionComponent)>();
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (pair.IsTestPrototype(p)) // Trauma - remove abstract check it doesnt see any
                    continue;

                if (p.HasComp(latheName))
                    latheProtos.Add(p);
                else if (p.TryComp<PhysicalCompositionComponent>(materialName, out var material))
                    materialEntityProtos.Add((p, material));
            }
            var compositionQuery = entMan.GetEntityQuery<PhysicalCompositionComponent>();
            // </Trauma>

            /* Trauma - this isnt needed anymore and it was fucking test run time from physics contact updates
            // Spawn all of the above material EntityPrototypes - we need actual entities to do whitelist checks
            var materialEntities = new List<EntityUid>(materialEntityProtos.Count());
            foreach (var materialEntityProto in materialEntityProtos)
            {
                materialEntities.Add(entMan.SpawnEntity(materialEntityProto.ID, mapData.GridCoords));
            }

            Assert.Multiple(() =>
            {
            */
                // Check each lathe individually
                foreach (var latheProto in latheProtos)
                {
                    if (!latheProto.TryComp<LatheComponent>(latheName, out var latheComp)) // Trauma - reuse name from above
                        continue;

                    if (!latheProto.TryComp<MaterialStorageComponent>(storageName, out var storageComp)) // Trauma - reuse name from above
                        continue;

                    // Test which material-containing entities are accepted by this lathe
                    var acceptedMaterials = new HashSet<ProtoId<MaterialPrototype>>();
                    foreach (var (materialEntity, compositionComponent) in materialEntityProtos) // Trauma - use the protoypes instead of spawned ents, it also has the comp now
                    {
                        //Assert.That(compositionQuery.TryComp(materialEntity, out var compositionComponent)); // Trauma - this is gotten once at the start
                        if (whitelistSystem.IsWhitelistFail(storageComp.Whitelist, materialEntity))
                            continue;

                        // Mark the lathe as accepting each material in the entity
                        foreach (var (material, _) in compositionComponent.MaterialComposition)
                        {
                            acceptedMaterials.Add(material);
                        }
                    }

                    // Collect all possible recipes assigned to this lathe
                    var recipes = new HashSet<ProtoId<LatheRecipePrototype>>();
                    latheSystem.AddRecipesFromPacks(recipes, latheComp.StaticPacks);
                    latheSystem.AddRecipesFromPacks(recipes, latheComp.DynamicPacks);
                    if (latheProto.TryComp<EmagLatheRecipesComponent>(emagName, out var emagRecipesComp)) // Trauma - reuse name from above
                    {
                        latheSystem.AddRecipesFromPacks(recipes, emagRecipesComp.EmagStaticPacks);
                        latheSystem.AddRecipesFromPacks(recipes, emagRecipesComp.EmagDynamicPacks);
                    }

                    // Check each recipe assigned to this lathe
                    foreach (var recipeId in recipes)
                    {
                        if (!protoMan.TryIndex(recipeId, out var recipeProto))
                        {
                            Assert.Fail($"Lathe recipe '{recipeId}' does not exist");
                            continue;
                        }

                        // Track the total material volume of the recipe
                        var totalQuantity = 0;
                        // Check each material called for by the recipe
                        foreach (var (materialId, quantity) in recipeProto.Materials)
                        {
                            Assert.That(protoMan.HasIndex(materialId), $"Material '{materialId}' does not exist");
                            // Make sure the material is accepted by the lathe
                            Assert.That(acceptedMaterials, Does.Contain(materialId), $"Lathe {latheProto.ID} has recipe {recipeId} but does not accept any materials containing {materialId}");
                            totalQuantity += quantity;
                        }
                        // Make sure the recipe doesn't call for more material than the lathe can hold
                        if (storageComp.StorageLimit != null)
                            Assert.That(totalQuantity, Is.LessThanOrEqualTo(storageComp.StorageLimit), $"Lathe {latheProto.ID} has recipe {recipeId} which calls for {totalQuantity} units of materials but can only hold {storageComp.StorageLimit}");
                    }
                }
        /* Trauma
            });
        });
        */
    }

    [Test]
    public async Task AllLatheRecipesValidTest()
    {
        var pair = Pair;

        var server = pair.Server;
        var proto = server.ProtoMan;

        Assert.Multiple(() =>
        {
            foreach (var recipe in proto.EnumeratePrototypes<LatheRecipePrototype>())
            {
                if (recipe.Result == null)
                    Assert.That(recipe.ResultReagents, Is.Not.Null, $"Recipe '{recipe.ID}' has no result or result reagents.");
            }
        });
    }
}
