using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Puller;

#nullable enable

[TestFixture]
public sealed class PullerTest : GameTest
{
    /// <summary>
    /// Checks that needsHands on PullerComponent is not set on mobs that don't even have hands.
    /// </summary>
    [Test]
    public async Task PullerSanityTest()
    {
        var pair = Pair;
        var server = pair.Server;

        // <Trauma> - microoptimisation shit
        var compFactory = server.EntMan.ComponentFactory;
        var protoManager = server.ProtoMan;
        var handsName = compFactory.GetComponentName<HandsComponent>();
        var pullerName = compFactory.GetComponentName<PullerComponent>();
        // </Trauma>

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<PullerComponent>(pullerName, out var puller)) // Trauma - use cached pullerName from above
                        continue;

                    if (!puller.NeedsHands)
                        continue;

                    Assert.That(proto.Components.ContainsKey(handsName), $"Found puller {proto} with NeedsHand pulling but has no hands?"); // Trauma - used cached handsName from above
                }
            });
        });
    }
}
