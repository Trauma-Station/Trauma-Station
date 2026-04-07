using System;
using System.Collections.Generic;
using System.Text;
using Content.Trauma.Common.Attribute;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Attribute.Components;

namespace Content.Trauma.Shared.Attribute.Systems;

public sealed partial class MalusAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthFeatTierdownComponent, GetStrengthFeatEvent>(OnMalus);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StrengthFeatTierdownComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            comp.Mod -= frameTime;
            if (comp.Mod < 0)
                RemCompDeferred<StrengthFeatTierdownComponent>(ent);
        }
    }

    private void OnMalus(Entity<StrengthFeatTierdownComponent> ent, ref GetStrengthFeatEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod); //Go for a ceiling because this is a malus.
    }
}
