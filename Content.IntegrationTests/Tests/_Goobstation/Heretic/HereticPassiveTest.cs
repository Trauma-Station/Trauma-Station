// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Prototypes;
using System.Linq;

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
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                var paths = Enum.GetValuesAsUnderlyingType<HereticPath>().Cast<HereticPath>();
                foreach (var path in paths)
                {
                    for (var i = 1; i <= 3; i++)
                    {
                        var id = $"{path}Passive{i}";
                        Assert.That(SProtoMan.HasIndex<HereticKnowledgePrototype>(id),
                            Is.True,
                            $"Heretic passive {id} does not exist");
                    }
                }
            });
        });
    }
}
