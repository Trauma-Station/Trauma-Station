// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Shared.Body;
using Content.Shared.Body;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

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

    // TODO: more stuff!
}
