// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Prototypes;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Prototypes;

namespace Content.IntegrationTests.Tests._Goobstation.Heretic;

/// <summary>
/// Make sure that t1/t2/t3 passive exists for each heretic path
/// </summary>
[TestFixture]
public sealed class HereticPassiveTest : GameTest
{
    [Test]
    public async Task ValidatePassives()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var paths = Enum.GetValuesAsUnderlyingType<HereticPath>().Cast<HereticPath>();
                foreach (var path in paths)
                {
                    for (var i = 1; i <= 3; i++)
                    {
                        var id = $"{path}Passive{i}";
                        Assert.That(protoMan.HasIndex<HereticKnowledgePrototype>(id),
                            Is.True,
                            $"Heretic passive {id} does not exist");
                    }
                }
            });
        });
    }
}
