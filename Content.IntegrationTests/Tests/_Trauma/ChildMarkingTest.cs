using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid.Markings;

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
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var marking in SProtoMan.EnumeratePrototypes<MarkingPrototype>())
                {
                    foreach (var suffix in marking.ChildMarkingsSuffix)
                    {
                        var id = $"{marking.ID}{suffix}";
                        Assert.That(SProtoMan.HasIndex<MarkingPrototype>(id),
                            Is.True,
                            $"Child marking {id} does not exist.");
                    }
                }
            });
        });
    }
}
