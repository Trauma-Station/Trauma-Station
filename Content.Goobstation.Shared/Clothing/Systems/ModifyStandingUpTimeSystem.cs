// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Stunnable;

namespace Content.Goobstation.Shared.Clothing.Systems;

public sealed partial class MultiplyStandingUpTimeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.SubscribeWithRelay<ModifyStandingUpTimeComponent, GetStandUpTimeEvent>(OnGetTime, held: false);
        SubscribeLocalEvent<ModifyStandingUpTimeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ModifyStandingUpTimeComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ClothingComponent>(ent))
            return;

        var msg = Loc.GetString("clothing-modify-stand-up-time-examine",
            ("mod", MathF.Round((1f - ent.Comp.Multiplier) * 100)));
        args.PushMarkup(msg);
    }

    private void OnGetTime(Entity<ModifyStandingUpTimeComponent> ent, ref GetStandUpTimeEvent args)
    {
        args.DoAfterTime *= ent.Comp.Multiplier;
    }
}
