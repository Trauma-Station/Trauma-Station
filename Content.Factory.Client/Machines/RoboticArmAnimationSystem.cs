// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Shared.Machines;
using Robust.Shared.Timing;

namespace Content.Factory.Client.Machines;

/// <summary>
/// Animations robotic arm's arm layer swinging.
/// Can't be done with engine AnimationPlayer as it can't animate individual layers.
/// </summary>
public sealed partial class RoboticArmAnimationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<RoboticArmComponent>();
        foreach (var ent in query)
        {
            if (ent.Comp.ItemSlot == null)
                continue;

            if (ent.Comp.NextMove is {} nextMove)
                Animate(ent, nextMove);
            else
                Reset(ent);
        }
    }

    private void Animate(Entity<RoboticArmComponent> ent, TimeSpan nextMove)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var started = nextMove - ent.Comp.MoveDelay;
        // 0-1 unless something weird happens
        var progress = (_timing.CurTime - started) / ent.Comp.MoveDelay;
        if (!ent.Comp.HasItem) // returning to the resting position when emptied
            progress = 1f - progress;
        var angle = Angle.FromDegrees(progress * 180f);
        sprite.LayerSetRotation(RoboticArmLayers.Arm, angle);
    }

    private void Reset(Entity<RoboticArmComponent> ent)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        var angle = ent.Comp.HasItem ? new Angle(Math.PI) : Angle.Zero;
        sprite.LayerSetRotation(RoboticArmLayers.Arm, angle);
    }
}
