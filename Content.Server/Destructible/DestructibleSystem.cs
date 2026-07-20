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
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Trigger.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Destructible;

[UsedImplicitly]
public sealed partial class DestructibleSystem : SharedDestructibleSystem
{
    // Trauma - moved a bunch of this to shared i hate this

    /// <summary>
    /// Minimum damage to invoke overkill behavior.
    /// </summary>
    private const int MinimumOverkill = 100;

    /// <summary>
    /// Multiplier over normal damage to invoke overkill.
    /// </summary>
    private const double OverkillMultiplier = 2.0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DestructibleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(OnDamageChanged);
    }

    /// <summary>
    /// Map Initialization function for <see cref="DestructibleComponent"/>, adding automatic overkill threshold.
    /// </summary>
    /// <param name="entity">The uid, component tuple.</param>
    /// <param name="args">The event arguments.</param>
    private void OnMapInit(Entity<DestructibleComponent> entity, ref MapInitEvent args)
    {
        AddOverkillThreshold(entity);
    }

    /// <summary>
    /// Check if any thresholds were reached. if they were, execute them.
    /// </summary>
    private void OnDamageChanged(Entity<DestructibleComponent> entity, ref DamageChangedEvent args)
    {
        var (uid, comp) = entity;

        comp.IsBroken = false;

        foreach (var threshold in comp.Thresholds)
        {
            if (Triggered(threshold, (uid, args.Damageable), entity.Comp.Scale)) // Trauma - add scale
            {
                RaiseLocalEvent(uid, new DamageThresholdReached(comp, threshold), true);

                var logImpact = LogImpact.Low;
                // Convert behaviors into string for logs
                var triggeredBehaviors = string.Join(", ", threshold.Behaviors.Select(behavior =>
                {
                    if (logImpact <= behavior.Impact)
                        logImpact = behavior.Impact;
                    if (behavior is DoActsBehavior doActsBehavior)
                    {
                        return $"{behavior.GetType().Name}:{doActsBehavior.Acts.ToString()}";
                    }
                    return behavior.GetType().Name;
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
                comp.IsBroken |= threshold.Behaviors.Any(b => b is DoActsBehavior doActsBehavior &&
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
    public bool Triggered(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner,
        FixedPoint2 scale) // Trauma
    {
        if (threshold.Trigger == null)
            return false;

        if (threshold.Triggered && threshold.TriggersOnce)
            return false;

        if (threshold.OldTriggered)
        {
            threshold.OldTriggered = threshold.Trigger.Reached(owner, this, scale); // Trauma - pass scale
            return false;
        }

        if (!threshold.Trigger.Reached(owner, this, scale)) // Trauma - pass scale
            return false;

        threshold.OldTriggered = true;
        return true;
    }

    /// <summary>
    /// Check if the conditions for the given threshold are currently true.
    /// </summary>
    public bool Reached(DamageThreshold threshold, Entity<Shared.Damage.Components.DamageableComponent> owner,
        FixedPoint2 scale) // Trauma
    {
        if (threshold.Trigger == null)
            return false;

        return threshold.Trigger.Reached(owner, this, scale); // Trauma - pass scale
    }

    /// <summary>
    /// Triggers this threshold.
    /// </summary>
    /// <param name="threshold">The threshold to execute.</param>
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

    /// <summary>
    /// Adds a threshold to the threshold list. If the entity does not have a destructible component, one will be added.
    /// </summary>
    /// <param name="entity">The entity, component tuple to target.</param>
    /// <param name="threshold">The threshold to add.</param>
    /// <param name="index">The index at which to insert the threshold.</param>
    public void AddThreshold(Entity<DestructibleComponent?> entity, DamageThreshold threshold, Index? index)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            entity.Comp = AddComp<DestructibleComponent>(entity.Owner);

        if (index is not null)
        {
            var threshIndex = index.Value.GetOffset(entity.Comp.Thresholds.Count);
            entity.Comp.Thresholds.Insert(threshIndex, threshold);
        }
        else
        {
            entity.Comp.Thresholds.Add(threshold);
        }
    }

    /// <summary>
    /// Adds an overkill threshold if one does not exist.
    /// </summary>
    /// <remarks>
    /// An overkill threshold is a top priority threshold that will destroy the entity without triggering any other
    /// behaviors applied to the entity.
    /// </remarks>
    /// <param name="entity">The entity, component tuple to target.</param>
    private void AddOverkillThreshold(Entity<DestructibleComponent> entity)
    {
        if (!entity.Comp.GenerateOverkillThreshold)
            return;

        var maxTrigger = FixedPoint2.Zero;

        foreach (var threshold in entity.Comp.Thresholds)
        {
            if (threshold.Trigger is not DamageTrigger trigger)
                continue;

            foreach (var behavior in threshold.Behaviors)
            {
                // Not a destruction behavior
                if (behavior is not DoActsBehavior actBehavior || !actBehavior.HasAct(ThresholdActs.Destruction))
                    continue;

                // Already has a pure destruction behavior
                if (threshold.Behaviors.Count == 1)
                    return;

                maxTrigger = FixedPoint2.Max(maxTrigger, trigger.Damage);
            }
        }

        // No destruction behavior
        if (FixedPoint2.Zero == maxTrigger)
            return;

        var autoThreshold = new DamageThreshold
        {
            Trigger = new DamageTrigger { Damage = FixedPoint2.Max(MinimumOverkill, OverkillMultiplier * maxTrigger) },
            Behaviors = { new DoActsBehavior { Acts = ThresholdActs.Destruction } },
        };

        // Thresholds are evaluated in order, so overkill must be first to avoid triggering effects
        AddThreshold(entity.AsNullable(), autoThreshold, 0);
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
