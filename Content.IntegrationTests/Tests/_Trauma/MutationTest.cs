// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Genetics.Abilities;
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
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var mutation = entMan.System<MutationSystem>();
        var factory = entMan.ComponentFactory;
        // monkey polymorph mutation messes it up so exclude it
        var blacklisted = factory.GetComponentName<PolymorphMutationComponent>();

        var mobs = new List<EntityUid>();
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in mutation.AllMutations.Keys)
                {
                    if (!protoMan.Resolve(id, out var proto) || proto.Components.ContainsKey(blacklisted))
                        continue;

                    var mob = entMan.SpawnEntity(TestMob, map.GridCoords);
                    Assert.That(mutation.AddMutation(mob, id), $"Failed to add {id} to {entMan.ToPrettyString(mob)}");
                    Assert.That(mutation.HasMutation(mob, id), $"Added {id} but it was not present in {entMan.ToPrettyString(mob)}");
                    mobs.Add(mob);
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
