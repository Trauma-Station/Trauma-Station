// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Void;

public sealed class SlowdownResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<SlowdownResistanceComponent, BeforeMovespeedModifierAppliedEvent>(
            OnBeforeModifierApplied,
            held: false);

        SubscribeLocalEvent<SlowdownResistanceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<SlowdownResistanceComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<MovementSpeedModifierComponent>(ent))
            return;

        var reduction = MathF.Round(ent.Comp.Reduction * 100f);
        var loc = ent.Comp.Global
            ? "slowdown-resistance-component-examine-message-global"
            : "slowdown-resistance-component-examine-message-local";

        args.PushMarkup(Loc.GetString(loc, ("reduction", reduction)));
    }

    private void OnBeforeModifierApplied(Entity<SlowdownResistanceComponent> ent, ref BeforeMovespeedModifierAppliedEvent args)
    {
        var reduction = ent.Comp.Global
            ? ent.Comp.Reduction
            : TryComp(ent, out ClothingSpeedModifierComponent? mod)
                ? MathF.Min(ent.Comp.Reduction, MathF.Max(1f - mod.SprintModifier, 1f - mod.WalkModifier))
                : 0f;

        args.WalkModifier = ModifySlowdown(args.WalkModifier, reduction);
        args.SprintModifier = ModifySlowdown(args.SprintModifier, reduction);
    }

    private float ModifySlowdown(float movementModifier, float reduction)
    {
        return MathF.Min(MathF.Max(1f, movementModifier), movementModifier + reduction);
    }
}
