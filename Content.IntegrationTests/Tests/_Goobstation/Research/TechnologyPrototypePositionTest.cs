// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Numerics;

namespace Content.IntegrationTests.Tests._Goobstation.Research;

[TestFixture]
public sealed class TechnologyPrototypePositionTests : GameTest
{
    [Test]
    public async Task TechnologyPrototypePositionsAreUniqueTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        var fails = new List<string>();
        var positions = new HashSet<Vector2>();

        await server.WaitAssertion(() =>
        {
            foreach (var techProto in protoManager.EnumeratePrototypes<TechnologyPrototype>())
            {
                Vector2 position = techProto.Position;

                if (!positions.Add(position))
                    fails.Add($"ID: {techProto.ID} Position - {position}");
            }
        });

        if (fails.Count > 0)
        {
            var msg = string.Join("\n", fails) + "\n" + "Found duplicate positions for following" + nameof(TechnologyPrototype);
            Assert.Fail(msg);
        }
    }
}
