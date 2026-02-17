// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure that every TargetAction has an event specified for either Entity or World.
/// Prevents runtime errors when someone tries to use the action.
/// </summary>
[TestFixture]
public sealed class ActionEventTest
{
    /// <summary>
    /// Actions which have their event set at runtime, these won't be checked for validity
    /// as it's intentionally null in the prototype.
    /// </summary>
    public static readonly EntProtoId[] ActionBlacklist =
    [
        "BaseMappingDecalAction"
    ];

    [Test]
    public async Task CheckTargetActions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var factory = server.EntMan.ComponentFactory;
        var protoMan = server.ProtoMan;

        var actionName = factory.GetComponentName<ActionComponent>();
        var entName = factory.GetComponentName<EntityTargetActionComponent>();
        var targetName = factory.GetComponentName<TargetActionComponent>();
        var worldName = factory.GetComponentName<WorldTargetActionComponent>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.Components.ContainsKey(targetName) || ActionBlacklist.Contains(proto.ID))
                        continue; // only process actions with TargetAction

                    Assert.That(proto.Components.ContainsKey(actionName), $"Action {proto.ID} is missing ActionComponent");

                    var hasEnt = proto.TryGetComponent<EntityTargetActionComponent>(entName, out var entTarget);
                    var hasWorld = proto.TryGetComponent<WorldTargetActionComponent>(worldName, out var worldTarget);
                    var entAction = hasEnt && entTarget.Event != null;
                    var worldAction = hasWorld && worldTarget.Event != null;

                    if (hasEnt && hasWorld)
                    {
                        // if both components are present, the event must be on WorldTargetActionComponent
                        Assert.That(!entAction, $"Action {proto.ID} cannot have events set for both Entity and World targets!");
                        Assert.That(worldAction, $"Action {proto.ID} needs an event set for WorldTargetActionComponent because of multi-targeting");
                    }
                    else if (hasEnt)
                    {
                        // only entity target, just check it has an event
                        Assert.That(entAction, $"Action {proto.ID} has no event set for EntityTargetActionComponent!");
                    }
                    else if (hasWorld)
                    {
                        // only world target, just check it has an event
                        Assert.That(worldAction, $"Action {proto.ID} has no event set for WorldTargetActionComponent!");
                    }
                    else
                    {
                        // currently there are no other supported types for target actions.
                        Assert.Fail($"Action {proto.ID} had TargetActionComponent but not Entity or World targets!");
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
