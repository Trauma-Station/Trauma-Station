using System.Collections.Generic;
using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Prototypes;
using Content.Shared.Research.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

[TestFixture]
public sealed class LatheTest
{
    [Test]
    public async Task TestLatheRecipeIngredientsFitLathe()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapData = await pair.CreateTestMap();

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var materialStorageSystem = server.System<SharedMaterialStorageSystem>();
        var whitelistSystem = server.System<EntityWhitelistSystem>();
        var latheSystem = server.System<SharedLatheSystem>();

        await server.WaitAssertion(() =>
        {
            // Find all the lathes
            // <Trauma> - remove linq jesus christ
            var latheName = compFactory.GetComponentName<LatheComponent>();
            var materialName = compFactory.GetComponentName<PhysicalCompositionComponent>();
            var latheProtos = new List<EntityPrototype>();
            var materialEntityProtos = new List<EntityPrototype>();
            foreach (var p in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (p.Abstract || pair.IsTestPrototype(p))
                    continue;

                if (p.Components.ContainsKey(latheName))
                    latheProtos.Add(p);
                else if (p.Components.ContainsKey(materialName))
                    materialEntityProtos.Add(p);
            }
            var compositionQuery = entMan.GetEntityQuery<PhysicalCompositionComponent>();
            // </Trauma>

            // Spawn all of the above material EntityPrototypes - we need actual entities to do whitelist checks
            var materialEntities = new List<EntityUid>(materialEntityProtos.Count); // Trauma - remove () from Count it's a list now
            foreach (var materialEntityProto in materialEntityProtos)
            {
                materialEntities.Add(entMan.SpawnEntity(materialEntityProto.ID, mapData.GridCoords));
            }

            Assert.Multiple(() =>
            {
                // Check each lathe individually
                foreach (var latheProto in latheProtos)
                {
                    if (!latheProto.TryGetComponent<LatheComponent>(out var latheComp, compFactory))
                        continue;

                    if (!latheProto.TryGetComponent<MaterialStorageComponent>(out var storageComp, compFactory))
                        continue;

                    // Test which material-containing entities are accepted by this lathe
                    var acceptedMaterials = new HashSet<ProtoId<MaterialPrototype>>();
                    foreach (var materialEntity in materialEntities)
                    {
                        Assert.That(compositionQuery.TryComp(materialEntity, out var compositionComponent)); // Trauma - use query from above
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
                    if (latheProto.TryGetComponent<EmagLatheRecipesComponent>(out var emagRecipesComp, compFactory))
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
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AllLatheRecipesValidTest()
    {
        await using var pair = await PoolManager.GetServerClient();

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

        await pair.CleanReturnAsync();
    }
}
