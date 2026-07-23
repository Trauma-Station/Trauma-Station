using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid.Markings;
using Content.Trauma.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Make sure child markings exist for markings that have them defined
/// </summary>
[TestFixture]
public sealed class ChildMarkingTest : GameTest
{
    [Test]
    public async Task ValidateChildMarkings()
    {
        var pair = Pair;
        var server = pair.Server;

        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var  marking in protoMan.EnumeratePrototypes<MarkingPrototype>())
                {
                    foreach (var suffix in marking.ChildMarkingsSuffix)
                    {
                        var id = $"{marking.ID}{suffix}";
                        Assert.That(protoMan.HasIndex<HereticKnowledgePrototype>(id),
                            Is.True,
                            $"Child marking {id} does not exist.");
                    }
                }
            });
        });
    }
}
