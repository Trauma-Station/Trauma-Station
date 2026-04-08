using Content.Trauma.Common.Knowledge;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute.Components;

namespace Content.Trauma.Shared.Knowledge.Attribute.Attribute.Systems;

public sealed partial class MalusAttributeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrengthFeatTierdownComponent, GetStrengthFeatEvent>(OnStrengthFeatMalus);
        SubscribeLocalEvent<DefenseTierdownComponent, GetDefenseModifierEvent>(OnDefenseMalus);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var strQuery = EntityQueryEnumerator<StrengthFeatTierdownComponent>();
        while (strQuery.MoveNext(out var ent, out var comp))
        {
            comp.Mod -= frameTime;
            Dirty(ent, comp);
            if (comp.Mod < 0)
                RemCompDeferred<StrengthFeatTierdownComponent>(ent);
        }

        var defQuery = EntityQueryEnumerator<DefenseTierdownComponent>();
        while (defQuery.MoveNext(out var ent, out var comp))
        {
            comp.Mod -= frameTime;
            Dirty(ent, comp);
            if (comp.Mod < 0)
                RemCompDeferred<DefenseTierdownComponent>(ent);
        }
    }

    private void OnStrengthFeatMalus(Entity<StrengthFeatTierdownComponent> ent, ref GetStrengthFeatEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod); //Go for a ceiling because this is a malus.
    }

    private void OnDefenseMalus(Entity<DefenseTierdownComponent> ent, ref GetDefenseModifierEvent args)
    {
        args.Mod -= (int) Math.Ceiling(ent.Comp.Mod);
    }
}
