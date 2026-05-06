// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;
using Content.Trauma.Shared.Knowledge.Miscellanious.Systems;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

public sealed partial class MalusAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthFeatTierdownComponent, GetStrengthFeatEvent>(OnStrengthFeatMalus);
        SubscribeLocalEvent<DefenseTierdownComponent, GetDefenseDice>(OnDefenseMalus, after: [typeof(CombatSystem)]);
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

        var defQuery = EntityQueryEnumerator<DefenseTierdownComponent>();
        while (defQuery.MoveNext(out var ent, out var comp))
        {
            comp.Mod = Math.Min(comp.Mod, 6.0f);
            comp.Mod -= frameTime / 1.5f;
            Dirty(ent, comp);
            if (comp.Mod < 0)
                RemCompDeferred<DefenseTierdownComponent>(ent);
        }
    }

    private void OnStrengthFeatMalus(Entity<StrengthFeatTierdownComponent> ent, ref GetStrengthFeatEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod); //Go for a ceiling because this is a malus.
    }

    private void OnDefenseMalus(Entity<DefenseTierdownComponent> ent, ref GetDefenseDice args)
    {
        args.Dice -= (int) Math.Ceiling(ent.Comp.Mod); // Beatdown
    }
}
