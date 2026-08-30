using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
//using Content.Shared.Prototypes; // Trauma - die
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests to see if any entity prototypes specify solution fill level sprites that don't exist.
/// </summary>
[TestFixture]
public sealed class FillLevelSpriteTest : GameTest
{
    private static readonly string[] HandStateNames = ["left", "right"];
    private static readonly string[] EquipStateNames = ["back", "suitstorage"];

    [Test]
    public async Task FillLevelSpritesExist()
    {
        var pair = Pair;
        var client = pair.Client;
        // <Trauma> - microoptimisation
        var protoMan = CProtoMan;
        var entMan = CEntMan;
        var factory = entMan.ComponentFactory;
        var appearanceName = factory.CompName<AppearanceComponent>();
        var spriteName = factory.CompName<SpriteComponent>();
        var visualsName = factory.CompName<SolutionContainerVisualsComponent>();
        // </Trauma>
        var spriteSystem = client.System<SpriteSystem>();

        await client.WaitAssertion(() =>
        {
            // <Trauma> - optimise this shit
            /* remove protos here, no need to sort it or allocate a list at all
            var protos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.TryComp<SolutionContainerVisualsComponent>(out _, componentFactory))
                .OrderBy(p => p.ID)
                .ToList();
            */

            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    // get relevant prototype data here
                    if (!proto.TryComp<SolutionContainerVisualsComponent>(visualsName, out var visuals) ||
                        pair.IsTestPrototype(proto))
                        continue;

                    // use CompNames from above
                    Assert.That(proto.TryComp<SpriteComponent>(spriteName, out var sprite));
                    if (!proto.HasComp(appearanceName))
                    {
                        Assert.Fail(@$"{proto.ID} has SolutionContainerVisualsComponent but no AppearanceComponent.");
                    }

                    // Test base sprite fills
                    if (!string.IsNullOrEmpty(visuals.FillBaseName) && visuals.MaxFillLevels > 0)
                    {
                        var entity = entMan.Spawn(proto.ID);
                        if (!spriteSystem.LayerMapTryGet(entity, SolutionContainerLayers.Fill, out var fillLayerId, false))
                        {
                            Assert.Fail(@$"{proto.ID} has SolutionContainerVisualsComponent but no fill layer map.");
                        }
                        if (!spriteSystem.TryGetLayer(entity, fillLayerId, out var fillLayer, false))
                        {
                            Assert.Fail(@$"{proto.ID} somehow lost a layer.");
                        }
                        var rsi = fillLayer.ActualRsi;

                        for (var i = 1; i <= visuals.MaxFillLevels; i++)
                        {
                            var state = $"{visuals.FillBaseName}{i}";
                            Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has SolutionContainerVisualsComponent with
                                MaxFillLevels = {visuals.MaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                        }
                    }

                    // Test inhand sprite fills
                    if (!string.IsNullOrEmpty(visuals.InHandsFillBaseName) && visuals.InHandsMaxFillLevels > 0)
                    {
                        var rsi = sprite.BaseRSI;
                        for (var i = 1; i <= visuals.InHandsMaxFillLevels; i++)
                        {
                            foreach (var handname in HandStateNames)
                            {
                                var state = $"inhand-{handname}{visuals.InHandsFillBaseName}{i}";
                                Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has SolutionContainerVisualsComponent with
                                    InHandsMaxFillLevels = {visuals.InHandsMaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                            }
                        }
                    }

                    // Test equipped sprite fills
                    if (!string.IsNullOrEmpty(visuals.EquippedFillBaseName) && visuals.EquippedMaxFillLevels > 0)
                    {
                        var rsi = sprite.BaseRSI;
                        for (var i = 1; i <= visuals.EquippedMaxFillLevels; i++)
                        {
                            foreach (var equipName in EquipStateNames)
                            {
                                var state = $"equipped-{equipName}{visuals.EquippedFillBaseName}{i}";
                                Assert.That(rsi.TryGetState(state, out _), @$"{proto.ID} has SolutionContainerVisualsComponent with
                                    EquippedMaxFillLevels = {visuals.EquippedMaxFillLevels}, but {rsi.Path} doesn't have state {state}!");
                            }
                        }
                    }
                }
            });
            // <Trauma>
        });
    }
}
