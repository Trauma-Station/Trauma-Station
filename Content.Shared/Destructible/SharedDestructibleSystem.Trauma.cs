using Content.Shared.FixedPoint;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Gibbing;
using Content.Shared.Stacks;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Destructible;

/// <summary>
/// Trauma - some methods moved out of server
/// </summary>
public abstract partial class SharedDestructibleSystem
{
    [Dependency] public readonly IRobustRandom Random = default!;
    public new IEntityManager EntityManager => base.EntityManager;

    [Dependency] public readonly SharedAtmosphereSystem AtmosphereSystem = default!;
    [Dependency] public readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] public readonly GibbingSystem Gibbing = default!;
    [Dependency] public readonly SharedConstructionSystem ConstructionSystem = default!;
    [Dependency] public readonly SharedExplosionSystem ExplosionSystem = default!;
    [Dependency] public readonly SharedStackSystem StackSystem = default!;
    [Dependency] public readonly TriggerSystem TriggerSystem = default!;
    [Dependency] public readonly SharedSolutionContainerSystem SolutionContainerSystem = default!;
    [Dependency] public readonly SharedPuddleSystem PuddleSystem = default!;
    [Dependency] public readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] public readonly ISharedAdminLogManager AdminLogger = default!;

    public bool TryGetDestroyedAt(Entity<DestructibleComponent?> ent, [NotNullWhen(true)] out FixedPoint2? destroyedAt)
    {
        destroyedAt = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        destroyedAt = DestroyedAt(ent, ent.Comp);
        return true;
    }

    // FFS this shouldn't be this hard. Maybe this should just be a field of the destructible component. Its not
    // like there is currently any entity that is NOT just destroyed upon reaching a total-damage value.
    /// <summary>
    ///     Figure out how much damage an entity needs to have in order to be destroyed.
    /// </summary>
    /// <remarks>
    ///     This assumes that this entity has some sort of destruction or breakage behavior triggered by a
    ///     total-damage threshold.
    /// </remarks>
    public FixedPoint2 DestroyedAt(EntityUid uid, DestructibleComponent? destructible = null)
    {
        if (!Resolve(uid, ref destructible, logMissing: false))
            return FixedPoint2.MaxValue;

        // We have nested for loops here, but the vast majority of components only have one threshold with 1-3 behaviors.
        // Really, this should probably just be a property of the damageable component.
        var damageNeeded = FixedPoint2.MaxValue;
        foreach (var threshold in destructible.Thresholds)
        {
            if (threshold.Trigger is not DamageTrigger trigger)
                continue;

            foreach (var behavior in threshold.Behaviors)
            {
                if (behavior is DoActsBehavior actBehavior &&
                    actBehavior.HasAct(ThresholdActs.Destruction | ThresholdActs.Breakage))
                {
                    damageNeeded = FixedPoint2.Min(damageNeeded, trigger.Damage);
                }
            }
        }
        return damageNeeded;
    }
}
