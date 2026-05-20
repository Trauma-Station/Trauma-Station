// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;

public sealed class SlowdownResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlowdownResistanceComponent, BeforeMovespeedModifierAppliedEvent>(OnBeforeModifierApplied);
    }

    private void OnBeforeModifierApplied(Entity<SlowdownResistanceComponent> ent, ref BeforeMovespeedModifierAppliedEvent args)
    {
        args.WalkModifier = ModifySlowdown(args.WalkModifier, ent.Comp.Factor);
        args.SprintModifier = ModifySlowdown(args.SprintModifier, ent.Comp.Factor);
    }

    private float ModifySlowdown(float movementModifier, float factor)
    {
        if (movementModifier >= 1f)
            return movementModifier;
        var slowdown = 1f - movementModifier;
        var modified = slowdown * factor;
        return 1f - modified;
    }
}
