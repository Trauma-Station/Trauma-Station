using Content.IntegrationTests.Fixtures.Attributes;
using Content.Trauma.Shared.Hands;

namespace Content.IntegrationTests.Tests.Interaction;

public abstract partial class InteractionTest
{
    [SidedDependency(Side.Server)] protected PredictedHandsSystem SPredictedHands = default!;
}
