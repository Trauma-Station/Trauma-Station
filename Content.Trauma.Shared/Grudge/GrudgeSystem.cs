// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Trauma.Server.Grudges.Components;

namespace Content.Trauma.Shared.Grudge;

public sealed partial class GrudgeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrudgeItemComponent, ExaminedEvent>(OnExaminedItemGrudge);
    }


    private void OnExaminedItemGrudge(Entity<GrudgeItemComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Grudgee != args.Examiner)
            return;

        args.PushMarkup("This is your item!");
    }
}
