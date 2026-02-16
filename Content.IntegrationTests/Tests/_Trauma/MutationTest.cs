using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
[TestOf(typeof(MutationSystem))]
public sealed class MutationTest
{
    private static readonly EntProtoId TestMob = "MobHuman";

    /// <summary>
    /// Makes sure no errors happen when adding, updating and removing every mutation.
    /// Each mutation gets its own mob which is spawned on the same map.
    /// </summary>
    [Test]
    public async Task AddRemoveAllMutations()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var polymorphQuery = entMan.GetEntityQuery<PolymorphedEntityComponent>();
        var mutation = entMan.System<MutationSystem>();

        var mobs = new List<EntityUid>();
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in mutation.AllMutations.Keys)
                {
                    var mob = entMan.SpawnEntity(TestMob, map.GridCoords);
                    Assert.That(mutation.AddMutation(mob, id), $"Failed to add {id} to {entMan.ToPrettyString(mob)}");
                    // for monkified, the new monkey will have the mutation
                    var target = polymorphQuery.CompOrNull(mob)?.Parent ?? mob;
                    Assert.That(mutation.HasMutation(target, id), $"Added {id} but it was not present in {entMan.ToPrettyString(mob)}");
                    mobs.Add(mob);
                    if (target != mob)
                        mobs.Add(target); // delete the polymorphed entity too later
                }
            });
        });

        await server.WaitRunTicks(300); // 10 seconds

        await server.WaitAssertion(() =>
        {
            foreach (var mob in mobs)
            {
                mutation.ClearMutations(mob);
                entMan.DeleteEntity(mob);
            }
        });

        await server.WaitRunTicks(150); // 5 seconds

        await pair.CleanReturnAsync();
    }
}
