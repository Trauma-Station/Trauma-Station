// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Trauma;

[TestFixture]
public sealed class BodyTest
{
    /// <summary>
    /// Makes sure that every mob with a Body has a root part (torso).
    /// </summary>
    [Test]
    public async Task BodyRootPartExists()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var factory = entMan.ComponentFactory;
        var protoMan = server.ProtoMan;
        var partSys = entMan.System<BodyPartSystem>();

        var map = await pair.CreateTestMap();

        var bodyName = factory.GetComponentName<BodyComponent>();
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (pair.IsTestPrototype(proto) || !proto.Components.ContainsKey(bodyName))
                        continue;

                    var mob = entMan.SpawnEntity(proto.ID, map.GridCoords);
                    Assert.That(partSys.GetRootPart(mob), Is.Not.Null, $"{entMan.ToPrettyString(mob)} had no root part!");
                    entMan.DeleteEntity(mob);
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Makes sure that every mob with a Body can have all of its organs removed and restored, remaining the same.
    /// </summary>
    [Test]
    public async Task BodyRestoreTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var factory = entMan.ComponentFactory;
        var protoMan = server.ProtoMan;
        var bodySys = entMan.System<BodySystem>();
        var restoreSys = entMan.System<BodyRestoreSystem>();

        var map = await pair.CreateTestMap();

        var bodyName = factory.GetComponentName<BodyComponent>();
        var started = new HashSet<string>();
        var ended = new HashSet<string>();
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (pair.IsTestPrototype(proto) || !proto.Components.ContainsKey(bodyName))
                        continue;

                    var mob = entMan.SpawnEntity(proto.ID, map.GridCoords);
                    // get the starting list of organs
                    started.Clear();
                    foreach (var organ in bodySys.GetOrgans(mob))
                    {
                        started.Add(organ.Comp.Category);
                    }

                    // remove all non-root organs
                    foreach (var organ in bodySys.GetOrgans<ChildOrganComponent>(mob))
                    {
                        entMan.DeleteEntity(organ);
                    }

                    // restore them
                    restoreSys.RestoreBody(mob);

                    // get the new list of organs
                    ended.Clear();
                    foreach (var organ in bodySys.GetOrgans(mob))
                    {
                        ended.Add(organ.Comp.Category);
                    }

                    // make sure they are the same, or some organs were lost in the cycle
                    Assert.That(ended, Is.EquivalentTo(started),
                        $"{entMan.ToPrettyString(mob)} had different organs after having its body restored!");

                    entMan.DeleteEntity(mob);
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    // TODO: more stuff!
}
