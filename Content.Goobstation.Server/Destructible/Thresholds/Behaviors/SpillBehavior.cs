using Content.Goobstation.Shared.GameTicking;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Server.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class AddGameRuleBehavior : IThresholdBehavior
    {
        [DataField(required: true)]
        public EntProtoId Rule;

        public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
        {
            var ev = new AddGameRuleItemEvent(cause);
            system.EntityManager.EventBus.RaiseLocalEvent(owner, ref ev);

            system.EntityManager.System<GameTicker>().StartGameRule(Rule);
        }
    }
}
