using Content.Shared.Guardian.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Trauma.Shared.Guardian.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Guardian;

/// <summary>
/// Handles the passive fear aura of guardian types that have it, slowing nearby enemies.
/// Only active while the guardian is manifested, and never affects its host or other guardians.
/// </summary>
public sealed partial class GuardianFearAuraSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GuardianFearAuraComponent, GuardianComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var aura, out var guardian, out var xform))
        {
            if (!guardian.GuardianLoose || _timing.CurTime < aura.NextCheck)
                continue;

            aura.NextCheck = _timing.CurTime + aura.CheckInterval;

            var coords = _transform.GetMapCoordinates((uid, xform));
            foreach (var target in _lookup.GetEntitiesInRange<MobStateComponent>(coords, aura.Radius, LookupFlags.Dynamic))
            {
                if (target.Owner == uid || target.Owner == guardian.Host)
                    continue;

                _movementMod.TryUpdateMovementSpeedModDuration(
                    target.Owner,
                    aura.SlowdownEffect,
                    aura.SlowdownDuration,
                    aura.WalkSpeedModifier,
                    aura.SprintSpeedModifier);
            }
        }
    }
}
