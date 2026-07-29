// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma;

/// <summary>
/// Makes sure that every TargetAction has an event specified for either Entity or World.
/// Prevents runtime errors when someone tries to use the action.
/// </summary>
public sealed class ActionEventTest : GameTest
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
        var factory = SEntMan.ComponentFactory;

        var actionName = factory.CompName<ActionComponent>();
        var entName = factory.CompName<EntityTargetActionComponent>();
        var targetName = factory.CompName<TargetActionComponent>();
        var worldName = factory.CompName<WorldTargetActionComponent>();

        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in SProtoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.HasComp(targetName) || ActionBlacklist.Contains(proto.ID))
                        continue; // only process actions with TargetAction

                    Assert.That(proto.HasComp(actionName), $"Action {proto.ID} is missing ActionComponent");

                    var hasEnt = proto.TryComp<EntityTargetActionComponent>(entName, out var entTarget);
                    var hasWorld = proto.TryComp<WorldTargetActionComponent>(worldName, out var worldTarget);
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
    }
}
