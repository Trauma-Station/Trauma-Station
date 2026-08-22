// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Lavaland.Shared.Megafauna.Conditions.Targeting;

namespace Content.Lavaland.Shared.Megafauna.Conditions;

/// <summary>
/// Condition that returns true if the target is at specific range from the boss.
/// Returns false if out of range.
/// </summary>
public sealed partial class RangeCondition : MegafaunaEntityCondition
{
    [DataField]
    public float? MinRange;

    [DataField]
    public float? MaxRange;

    public override bool EvaluateImplementation(MegafaunaCalculationBaseArgs args, EntityUid target)
    {
        var entMan = args.EntMan;
        var xform = entMan.System<SharedTransformSystem>();

        var bossPos = xform.GetMapCoordinates(args.Entity);
        var targetPos = xform.GetMapCoordinates(target);

        if (bossPos.MapId != targetPos.MapId)
            return false;

        var distance = Vector2.Distance(bossPos.Position, targetPos.Position);
        return distance > (MinRange ?? -1f) && distance < (MaxRange ?? float.MaxValue);
    }
}
