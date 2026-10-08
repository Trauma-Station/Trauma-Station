// <Trauma>
using Content.Shared.Hands.Components;
// </Trauma>
#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Sprite;

/// <summary>
/// This test checks that all items have a visible sprite. The general rationale is that all items can be picked up
/// by players, thus they need to be visible and have a sprite that can be rendered on screen and in their hands GUI.
/// This has nothing to do with in-hand sprites.
/// </summary>
/// <remarks>
/// If a prototype fails this test, its probably either because it:
/// - Should be marked abstract
/// - inherits from BaseItem despite not being an item
/// - Shouldn't have an item component
/// - Is missing the required sprite information.
/// If none of the abveo are true, it might need to be added to the list of ignored components, see
/// <see cref="Ignored"/>
/// </remarks>
[TestFixture]
public sealed partial class ItemSpriteTest : GameTest // Trauma - correct copypaste major, made partial
{
    private static readonly HashSet<string> Ignored = new()
    {
        // The only prototypes that should get ignored are those that REQUIRE setup to get a sprite. At that point it is
        // the responsibility of the spawner to ensure that a valid sprite is set.
        "VirtualItem",
        "DetachedBody"
    };

    [Test]
    public async Task AllItemsHaveSpritesTest()
    {
        var pair = Pair;
        List<EntityPrototype> badPrototypes = [];

        var map = await Pair.CreateTestMap(); // Trauma
        await pair.Client.WaitPost(() =>
        {
            // <Trauma>
            var urist = CEntMan.SpawnEntity(Urist, map.CGridCoords);
            CEntMan.EnsureComponent<IgnoreUIRangeComponent>(urist); // avoid shitty UI debug assert
            var hands = CComp<HandsComponent>(urist);
            // </Trauma>
            foreach (var (proto, _) in pair.GetPrototypesWithComponent<ItemComponent>(Ignored))
            {
                var dummy = CEntMan.SpawnEntity(proto.ID, map.CGridCoords); // Trauma - spawn in a mapinit'd map instead of mapinit in nullspace
                var spriteComponent = pair.Client.EntMan.GetComponentOrNull<SpriteComponent>(dummy);
                if (spriteComponent?.Icon == null)
                    badPrototypes.Add(proto);
                // <Trauma> - equip it to try load inhand sprites, maybe wield too for wield sprites
                _hands.TryPickup(urist, dummy, checkActionBlocker: false, animate: false, handsComp: hands);
                _interaction.UseInHandInteraction(urist, dummy, false, false, false);
                // </Trauma>
                pair.Client.EntMan.DeleteEntity(dummy);
            }
            CEntMan.DeleteEntity(urist); // Trauma
        });

        Assert.Multiple(() =>
        {
            foreach (var proto in badPrototypes)
            {
                Assert.Fail($"Item prototype has no sprite: {proto.ID}. It should probably either be marked as abstract, not be an item, or have a valid sprite");
            }
        });
    }
}
