// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Actions;

public sealed partial class ActionProjectileAdvancedSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<ActiveActionProjectileAdvancedComponent, ActionProjectileAdvancedComponent>();
        while (eqe.MoveNext(out var uid, out var active, out var proj))
        {
            // Ensures we don't get fuck-ups with index out of bounds
            if (active.PatternIdx >= proj.ProjectilePatterns.Length)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            if (_timing.CurTime < active.NextPattern)
                continue;

            // Time's up, shoot the pattern towards the target
            ShootPattern(active.User, proj.ProjectilePatterns[active.PatternIdx], active.Target);

            // Advance the pattern here, and check if it's done so we can remove it
            active.PatternIdx++;
            if (GetCurrentPatternDelay((uid, proj), active.PatternIdx) is not { } newDelay)
            {
                RemCompDeferred(uid, active);
                continue;
            }

            active.NextPattern += newDelay;
            DirtyFields(uid, active, null, nameof(active.PatternIdx), nameof(active.NextPattern));
        }
    }

    [SubscribeLocalEvent]
    private void OnPerform(Entity<ActionProjectileAdvancedComponent> ent, ref ActionProjectileAdvancedEvent args)
    {
        if (ent.Comp.ProjectilePatterns.Length == 0)
            return;

        var active = EnsureComp<ActiveActionProjectileAdvancedComponent>(ent);
        active.PatternIdx = 0; // in case we already have the component, reset to 0 just in case

        var idx = active.PatternIdx;
        if (GetCurrentPatternDelay(ent, idx) is not { } patternDelay)
            return;

        active.NextPattern = _timing.CurTime + patternDelay;
        active.Target = args.Target;
        active.User = args.Performer;
        Dirty(ent);

        args.Handled = true;
    }

    /// <summary>
    /// Returns the current pattern delay, given an index.
    /// If the index is higher or equal to the array's length, then we return null, indicating that the patterns have been exhausted
    /// </summary>
    private TimeSpan? GetCurrentPatternDelay(Entity<ActionProjectileAdvancedComponent> ent, int index)
    {
        return index >= ent.Comp.ProjectilePatterns.Length
            ? null
            : ent.Comp.ProjectilePatterns[index].Delay;
    }

    /// <summary>
    /// Shoots the specified pattern towards the target
    /// </summary>
    private void ShootPattern(EntityUid user, ProjectileAdvancedPattern pattern, EntityUid target)
    {
        if (pattern.Amount == 0)
            return;

        var amount = pattern.Amount;

        var userPos = _transform.GetWorldPosition(user);
        var targetPos = _transform.GetWorldPosition(target);
        var targetAngle = (targetPos - userPos).ToWorldAngle();

        // We don't want to divide by 0 if it's only one bullet on the pattern
        var step = amount > 1 ? ( pattern.Angle / ( amount - 1 ) ) : 0f;
        var start = amount > 1 ? - ( pattern.Angle / 2f ) : 0f;

        var coords = Transform(user).Coordinates;
        for (var i = 0; i < amount; i++)
        {
            var angle = targetAngle + Angle.FromDegrees(start + step * i);

            // Finally, this is the direction that gets calculated with the spread angle + the target's angle
            var dir = angle.ToWorldVec();

            var proj = PredictedSpawnAtPosition(pattern.ProjProto, coords);
            _gun.ShootProjectile(proj, dir, Vector2.Zero, user, user, pattern.Speed);
        }
    }
}
