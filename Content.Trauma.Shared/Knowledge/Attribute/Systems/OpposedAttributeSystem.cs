// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

public sealed partial class OpposedAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthFeatOpposedComponent, GetStrengthFeatEvent>(OnStrengthFeatOpposed);
    }

    private void OnStrengthFeatOpposed(Entity<StrengthFeatOpposedComponent> ent, ref GetStrengthFeatEvent args)
    {
        args.Mod += ent.Comp.Mod;
    }
}
