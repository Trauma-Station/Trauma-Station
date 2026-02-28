// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
[TestOf(typeof(MutationSystem))]
public sealed class MutationTest
{
    private static readonly EntProtoId TestMob = "MobHuman";
    private static readonly EntProtoId TestMobPoly = "MobDwarf";
    private static readonly EntProtoId<MutationComponent> TestMutation = "MutationDwarfism";
    private static readonly ProtoId<PolymorphPrototype> TestPolymorph = "MutationMonkey";

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

    /// <summary>
    /// Checks that mutations are correctly transferred when polymorphing into another entity.
    /// </summary>
    [Test]
    public async Task MutationsPolymorphTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mutation = entMan.System<MutationSystem>();
        var polymorph = entMan.System<PolymorphSystem>();

        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var dorf = entMan.SpawnEntity(TestMobPoly, map.GridCoords);

            // dwarf must start with dwarfism
            Assert.That(mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not present in {entMan.ToPrettyString(dorf)}!");

            // return to monke
            if (polymorph.PolymorphEntity(dorf, TestPolymorph) is not {} monkey)
            {
                Assert.Fail($"Failed to polymorph {entMan.ToPrettyString(dorf)} into {TestPolymorph}!");
                return;
            }

            // the monkey must have taken all mutations
            Assert.That(mutation.HasMutation(monkey, TestMutation),
                $"{TestMutation} was not moved to {entMan.ToPrettyString(monkey)}!");
            Assert.That(!mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved from {entMan.ToPrettyString(dorf)}!");

            // return from monke
            Assert.That(polymorph.Revert(monkey), Is.EqualTo(dorf),
                $"Failed to revert polymorph from {entMan.ToPrettyString(monkey)} back to {entMan.ToPrettyString(dorf)}!");

            // dwarf should have his mutations back
            Assert.That(mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved back to {entMan.ToPrettyString(dorf)}!");

            entMan.DeleteEntity(dorf);
        });

        await pair.CleanReturnAsync();
    }
}
