using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ChangeConstructionNodeBehavior : IThresholdBehavior
    {
        [DataField(required: true)]
        public string Node { get; private set; } = string.Empty;

        public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
        {
            if (string.IsNullOrEmpty(Node))
                return;

            system.ConstructionSystem.ChangeNode(owner, null, Node, true);
        }
    }
}
