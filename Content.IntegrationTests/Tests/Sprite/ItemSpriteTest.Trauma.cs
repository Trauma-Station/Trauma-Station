using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;

namespace Content.IntegrationTests.Tests.Sprite;

public sealed partial class ItemSpriteTest
{
    private static readonly EntProtoId Urist = "MobHuman";

    [SidedDependency(Side.Client)] private SharedHandsSystem _hands = default!;
    [SidedDependency(Side.Client)] private SharedInteractionSystem _interaction = default!;
}
