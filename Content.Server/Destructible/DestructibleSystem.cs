// <Trauma>
using Content.Shared.Destructible.Thresholds;
using Content.Shared.Destructible.Thresholds.Behaviors;
// </Trauma>
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Construction;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Trigger.Systems;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public sealed partial class DestructibleSystem : SharedDestructibleSystem
    {
        // Trauma - moved a bunch of this to shared i hate this

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(OnDamageChanged);
        }

        /// <summary>
        /// Check if any thresholds were reached. if they were, execute them.
        /// </summary>
        private void OnDamageChanged(EntityUid uid, DestructibleComponent component, DamageChangedEvent args)
        {
            component.IsBroken = false;

            foreach (var threshold in component.Thresholds)
            {
                if (Triggered(threshold, (uid, args.Damageable)))
                {
                    RaiseLocalEvent(uid, new DamageThresholdReached(component, threshold), true);

                    var logImpact = LogImpact.Low;
                    // Convert behaviors into string for logs
                    var triggeredBehaviors = string.Join(", ", threshold.Behaviors.Select(b =>
                    {
                        if (logImpact <= b.Impact)
                            logImpact = b.Impact;
                        if (b is DoActsBehavior doActsBehavior)
                        {
                            return $"{b.GetType().Name}:{doActsBehavior.Acts.ToString()}";
                        }
                        return b.GetType().Name;
                    }));

                    // If it doesn't have a humanoid component, it's probably not particularly notable?
                    if (logImpact > LogImpact.Medium && !HasComp<HumanoidProfileComponent>(uid))
                        logImpact = LogImpact.Medium;

                    if (args.Origin != null)
                    {
                        AdminLogger.Add(LogType.Damaged,
                            logImpact,
                            $"{ToPrettyString(args.Origin.Value):actor} caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                    }
                    else
                    {
                        AdminLogger.Add(LogType.Damaged,
                            logImpact,
                            $"Unknown damage source caused {ToPrettyString(uid):subject} to trigger [{triggeredBehaviors}]");
                    }

                    Execute(threshold, uid, args.Origin);
                }

                if (threshold.OldTriggered)
                {
                    component.IsBroken |= threshold.Behaviors.Any(b => b is DoActsBehavior doActsBehavior &&
                        (doActsBehavior.HasAct(ThresholdActs.Breakage) || doActsBehavior.HasAct(ThresholdActs.Destruction)));
                }

                // if destruction behavior (or some other deletion effect) occurred, don't run other triggers.
                if (EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                    return;
            }
        }

        /// <summary>
        /// Check if the given threshold should trigger.
        /// </summary>
        public bool Triggered(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner)
        {
            if (threshold.Trigger == null)
                return false;

            if (threshold.Triggered && threshold.TriggersOnce)
                return false;

            if (threshold.OldTriggered)
            {
                threshold.OldTriggered = threshold.Trigger.Reached(owner, this);
                return false;
            }

            if (!threshold.Trigger.Reached(owner, this))
                return false;

            threshold.OldTriggered = true;
            return true;
        }

        /// <summary>
        /// Check if the conditions for the given threshold are currently true.
        /// </summary>
        public bool Reached(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner)
        {
            if (threshold.Trigger == null)
                return false;

            return threshold.Trigger.Reached(owner, this);
        }

        /// <summary>
        /// Triggers this threshold.
        /// </summary>
        /// <param name="owner">The entity that owns this threshold.</param>
        /// <param name="cause">The entity that caused this threshold to trigger.</param>
        public void Execute(DamageThreshold threshold, EntityUid owner, EntityUid? cause = null)
        {
            threshold.Triggered = true;

            foreach (var behavior in threshold.Behaviors)
            {
                // The owner has been deleted. We stop execution of behaviors here.
                if (!Exists(owner))
                    return;

                // TODO: Replace with EntityEffects.
                behavior.Execute(owner, this, cause);
            }
        }

        // Trauma - moved TryGetDestroyedAt and DestroyedAt to shared
    }

    // Currently only used for destructible integration tests. Unless other uses are found for this, maybe this should just be removed and the tests redone.
    /// <summary>
    ///     Event raised when a <see cref="DamageThreshold"/> is reached.
    /// </summary>
    public sealed class DamageThresholdReached : EntityEventArgs
    {
        public readonly DestructibleComponent Parent;

        public readonly DamageThreshold Threshold;

        public DamageThresholdReached(DestructibleComponent parent, DamageThreshold threshold)
        {
            Parent = parent;
            Threshold = threshold;
        }
    }
}
