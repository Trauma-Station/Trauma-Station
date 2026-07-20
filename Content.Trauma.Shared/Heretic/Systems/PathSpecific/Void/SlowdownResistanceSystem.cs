// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
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
        if (!HasComp<ClothingComponent>(ent))
            return;

        var reduction = MathF.Round(ent.Comp.Reduction * 100f);
        args.PushMarkup(Loc.GetString("slowdown-resistance-component-examine-message", ("reduction", reduction)));
    }

    private void OnBeforeModifierApplied(Entity<SlowdownResistanceComponent> ent, ref BeforeMovespeedModifierAppliedEvent args)
    {
        args.WalkModifier = ModifySlowdown(args.WalkModifier, ent.Comp.Reduction);
        args.SprintModifier = ModifySlowdown(args.SprintModifier, ent.Comp.Reduction);
    }

    private float ModifySlowdown(float movementModifier, float reduction)
    {
        return MathF.Min(MathF.Max(1f, movementModifier), movementModifier + reduction);
    }
}
