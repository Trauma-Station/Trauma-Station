// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Content.Goobstation.Common.Weapons.MeleeDash;
using Content.Trauma.Common.Weapons;

namespace Content.Trauma.Client.Weapons;

public sealed partial class MeleeDashSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeDashComponent, PreHeavyAttackEvent>(OnHeavyAttack);
    }

    private void OnHeavyAttack(Entity<MeleeDashComponent> ent, ref PreHeavyAttackEvent args)
    {
        if (args.Handled || !(args.Direction != Vector2.Zero))
            return;

        RaisePredictiveEvent(new MeleeDashEvent(GetNetEntity(ent.Owner), args.Direction));
        args.Handled = true;
    }
}
