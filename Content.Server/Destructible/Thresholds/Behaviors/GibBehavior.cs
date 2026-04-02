using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class GibBehavior : IThresholdBehavior
    {
        [DataField("recursive")] private bool _recursive = true;
        /// <summary>
        /// Trauma - delete the gibs afterwards if true.
        /// </summary>
        [DataField]
        public bool DeleteGibs;

        public LogImpact Impact => LogImpact.Extreme;

        public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
        {
            // <Trauma> - store gibs for deletion if enabled
            var gibs = system.Gibbing.Gib(owner, _recursive);
            if (DeleteGibs)
            {
                foreach (var gib in gibs)
                {
                    system.EntityManager.QueueDeleteEntity(gib);
                }
            }
            // </Trauma>
        }
    }
}
