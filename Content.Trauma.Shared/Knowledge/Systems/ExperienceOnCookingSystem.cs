// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Kitchen;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class ExperienceOnCookingSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, CookedFoodEvent>(_knowledge.RelayActiveEvent);
    }

    [SubscribeLocalEvent]
    private void OnCookedFood(Entity<ExperienceOnCookingComponent> ent, ref CookedFoodEvent args)
    {
        // increase limit for each unique recipe made
        if (ent.Comp.Cooked.Add(args.Result))
        {
            ent.Comp.Limit = ent.Comp.Cooked.Count;
            Dirty(ent);
        }

        // TODO: scale XP gain by the nutrition or something
        var xp = args.Count * ent.Comp.Scale;
        var limit = Math.Min(100, ent.Comp.Limit);
        _knowledge.AddExperience(ent.Owner, args.User, xp, limit);
    }
}
