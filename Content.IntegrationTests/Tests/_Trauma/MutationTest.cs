// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Server.Polymorph.Systems;
using Content.Shared.Polymorph;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Linq;

namespace Content.IntegrationTests.Tests._Trauma;

[TestOf(typeof(MutationSystem))]
public sealed class MutationTest : GameTest
{
    private static readonly EntProtoId TestMob = "MobHuman";
    private static readonly EntProtoId<MutationComponent> TestMutation = "MutationDwarfism";
    private static readonly ProtoId<PolymorphPrototype> TestPolymorph = "MutationMonkey";

    [SidedDependency(Side.Server)] private MutationSystem _mutation = default!;
    [SidedDependency(Side.Server)] private PolymorphSystem _polymorph = default!;
    [SidedDependency(Side.Server)] private ScannedGenomeSystem _genome = default!;

    /// <summary>
    /// Makes sure no errors happen when adding, updating and removing every mutation.
    /// Each mutation gets its own mob which is spawned on the same map.
    /// </summary>
    [Test]
    public async Task AddRemoveAllMutations()
    {
        var map = await Pair.CreateTestMap();

        var factory = SEntMan.ComponentFactory;
        // monkey polymorph mutation messes it up so exclude it
        var blacklisted = factory.CompName<PolymorphMutationComponent>();
        var mobs = new List<EntityUid>();
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var id in _mutation.AllMutations.Keys)
                {
                    if (!SProtoMan.Resolve(id, out var proto) || proto.HasComp(blacklisted))
                        continue;

                    var mob = SEntMan.SpawnEntity(TestMob, map.GridCoords);
                    Assert.That(_mutation.AddMutation(mob, id), $"Failed to add {id} to {SEntMan.ToPrettyString(mob)}");
                    Assert.That(_mutation.HasMutation(mob, id), $"Added {id} but it was not present in {SEntMan.ToPrettyString(mob)}");
                    mobs.Add(mob);
                }
            });
        });

        await RunSeconds(10);

        await Server.WaitAssertion(() =>
        {
            foreach (var mob in mobs)
            {
                _mutation.ClearMutations(mob);
                SEntMan.DeleteEntity(mob);
            }
        });

        await RunSeconds(2);
    }

    /// <summary>
    /// Checks that mutations are correctly transferred when polymorphing into another entity.
    /// </summary>
    [Test]
    public async Task MutationsPolymorphTest()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var dorf = SEntMan.SpawnEntity(TestMob, map.GridCoords);

            // scan him and compare sequences later
            _genome.ScanGenome(dorf);

            // make him short
            Assert.That(_mutation.AddMutation(dorf, TestMutation),
                $"Failed to give {SEntMan.ToPrettyString(dorf)} {TestMutation}!");
            Assert.That(_mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not present in {SEntMan.ToPrettyString(dorf)}!");
            var started = GetSequenceIds(dorf);
            Assert.That(started.Contains(TestMutation),
                $"{TestMutation} did not get added to already scanned genomes of {SEntMan.ToPrettyString(dorf)}!");

            // return to monke
            if (_polymorph.PolymorphEntity(dorf, TestPolymorph) is not {} monkey)
            {
                Assert.Fail($"Failed to polymorph {SEntMan.ToPrettyString(dorf)} into {TestPolymorph}!");
                return;
            }

            // the monkey must have taken all mutations
            Assert.That(_mutation.HasMutation(monkey, TestMutation),
                $"{TestMutation} was not moved to {SEntMan.ToPrettyString(monkey)}!");
            Assert.That(!_mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved from {SEntMan.ToPrettyString(dorf)}!");

            // and still have everything scanned
            var ended = GetSequenceIds(monkey);
            Assert.That(ended.Contains(TestMutation),
                $"{TestMutation} was not moved to scanned genomes of {SEntMan.ToPrettyString(monkey)}!");
            Assert.That(ended, Is.EquivalentTo(started),
                "Lost some scanned genome sequences when turning into a monkey!");

            // return from monke
            Assert.That(_polymorph.Revert(monkey), Is.EqualTo(dorf),
                $"Failed to revert polymorph from {SEntMan.ToPrettyString(monkey)} back to {SEntMan.ToPrettyString(dorf)}!");

            // dwarf should have his mutations back
            Assert.That(_mutation.HasMutation(dorf, TestMutation),
                $"{TestMutation} was not moved back to {SEntMan.ToPrettyString(dorf)}!");

            ended = GetSequenceIds(dorf);
            Assert.That(ended, Is.EquivalentTo(started),
                "Lost some scanned genome sequences when turning back from a monkey!");

            SEntMan.DeleteEntity(dorf);
        });
    }

    private List<string> GetSequenceIds(EntityUid uid)
    {
        var comp = SEntMan.GetComponent<ScannedGenomeComponent>(uid);
        var ids = new List<string>(comp.Sequences.Count);
        foreach (var sequence in comp.Sequences)
        {
            ids.Add(sequence.Mutation);
        }
        return ids;
    }
}
