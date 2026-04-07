using System;
using System.Collections.Generic;
using System.Text;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

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
