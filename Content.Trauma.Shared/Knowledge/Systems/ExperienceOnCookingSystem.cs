// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Kitchen;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed class ExperienceOnCookingSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, CookedFoodEvent>(_knowledge.RelayActiveEvent);
        SubscribeLocalEvent<ExperienceOnCookingComponent, CookedFoodEvent>(OnCookedFood);
    }

    private void OnCookedFood(Entity<ExperienceOnCookingComponent> ent, ref CookedFoodEvent args)
    {
        // TODO: scale XP gain by the nutrition or something
        var xp = args.Count * ent.Comp.Scale;
        // TODO: limit level by total unique foods you've made
        var limit = 100;
        _knowledge.AddExperience(ent.Owner, args.User, xp, limit);
    }
}
