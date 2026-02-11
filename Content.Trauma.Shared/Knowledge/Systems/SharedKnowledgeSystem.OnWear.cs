using System;
using System.Collections.Generic;
using System.Text;
using Content.Shared.Clothing;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    private void InitializeOnWear()
    {
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotEquippedEvent>(OnGrantKnowledgeWear);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotUnequippedEvent>(OnRemoveKnowledgeWear);
    }

    private void OnGrantKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var wearer = args.Wearer;

        if (TryGetKnowledgeEntity(args.Wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, skill.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, new KeyValuePair<EntProtoId, int>(skill.Key, 0));

            if (TryGetKnowledgeUnit(wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryLevel += skill.Value;
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, experience.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, new KeyValuePair<EntProtoId, int>(experience.Key, 0));

            if (TryGetKnowledgeUnit(wearer, experience.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryLevel += experience.Value;
        }

    }

    private void OnRemoveKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var wearer = args.Wearer;
        if (TryGetKnowledgeEntity(args.Wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(args.Wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
            {
                if (knowledgeComponent.Level <= 0)
                    TryRemoveKnowledgeUnit(args.Wearer, skill.Key);
                if (knowledgeComponent.TemporaryLevel - skill.Value < 0)
                    knowledgeComponent.TemporaryLevel = 0;
                else
                    knowledgeComponent.TemporaryLevel -= skill.Value;
            }
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(args.Wearer, experience.Key) is { } knowledgeUnit && TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent) && knowledgeComponent.Level <= 0)
                TryRemoveKnowledgeUnit(args.Wearer, experience.Key);

            if (TryGetKnowledgeUnit(args.Wearer, experience.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out knowledgeComponent))
                knowledgeComponent.TemporaryLevel -= experience.Value;
        }
    }
}
