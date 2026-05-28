// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

public sealed partial class MalusAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthFeatTierdownComponent, GetStrengthFeatEvent>(OnStrengthFeatMalus);
        SubscribeLocalEvent<AgilityFeatTierdownComponent, GetAgilityFeatEvent>(OnAgilityFeatMalus);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var strQuery = EntityQueryEnumerator<StrengthFeatTierdownComponent>();
        while (strQuery.MoveNext(out var ent, out var comp))
        {
            comp.Mod = Math.Min(comp.Mod, 6.0f);
            comp.Mod -= frameTime / 1.5f;
            Dirty(ent, comp);
            if (comp.Mod < 0)
                RemCompDeferred<StrengthFeatTierdownComponent>(ent);
        }

        var defQuery = EntityQueryEnumerator<AgilityFeatTierdownComponent>();
        while (defQuery.MoveNext(out var ent, out var comp))
        {
            comp.Mod = Math.Min(comp.Mod, 6.0f);
            comp.Mod -= frameTime / 1.5f;
            Dirty(ent, comp);
            if (comp.Mod < 0)
                RemCompDeferred<AgilityFeatTierdownComponent>(ent);
        }
    }

    private void OnStrengthFeatMalus(Entity<StrengthFeatTierdownComponent> ent, ref GetStrengthFeatEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod); //Go for a ceiling because this is a malus.
    }

    private void OnAgilityFeatMalus(Entity<AgilityFeatTierdownComponent> ent, ref GetAgilityFeatEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod);
    }
}
