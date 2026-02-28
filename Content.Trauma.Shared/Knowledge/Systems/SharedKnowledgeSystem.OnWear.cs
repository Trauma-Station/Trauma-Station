using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    private void InitializeOnWear()
    {
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, OrganGotInsertedEvent>(OnGrantKnowledgeOrgan);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, OrganGotRemovedEvent>(OnRemoveKnowledgeOrgan);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotEquippedEvent>(OnGrantKnowledgeWear);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotUnequippedEvent>(OnRemoveKnowledgeWear);
    }

    private void OnGrantKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotInsertedEvent args)
    {
        var wearer = args.Target;
        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, skill.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, (skill.Key, 0));

            if (TryGetKnowledgeUnit(wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryLevel += skill.Value;
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, experience.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, (experience.Key, 0));

            if (TryGetKnowledgeUnit(wearer, experience.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.BonusExperience += experience.Value;
        }
        foreach (var blocked in ent.Comp.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, blocked.Key) is { } knowledgeUnitActual && TryComp<MartialArtsKnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryBlockedCounter += 1;
        }
    }

    private void OnRemoveKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotRemovedEvent args)
    {
        var wearer = args.Target;
        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
            {
                if (knowledgeComponent.Level <= 0)
                    TryRemoveKnowledgeUnit(wearer, skill.Key);
                if (knowledgeComponent.TemporaryLevel - skill.Value < 0)
                    knowledgeComponent.TemporaryLevel = 0;
                else
                    knowledgeComponent.TemporaryLevel -= skill.Value;
            }
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, experience.Key) is { } knowledgeUnit && TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent))
            {
                if (knowledgeComponent.Level <= 0)
                    TryRemoveKnowledgeUnit(wearer, experience.Key);
                else
                    knowledgeComponent.BonusExperience -= experience.Value;
            }
        }
        foreach (var blocked in ent.Comp.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, blocked.Key) is { } knowledgeUnitActual && TryComp<MartialArtsKnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryBlockedCounter -= 1;
        }
    }

    private void OnGrantKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var wearer = args.Wearer;

        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, skill.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, (skill.Key, 0));

            if (TryGetKnowledgeUnit(wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryLevel += skill.Value;
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, experience.Key) is not { } knowledgeUnit)
                TryAddKnowledgeUnit(wearer, (experience.Key, 0));

            if (TryGetKnowledgeUnit(wearer, experience.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.BonusExperience += experience.Value;
        }
        foreach (var blocked in ent.Comp.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, blocked.Key) is { } knowledgeUnitActual && TryComp<MartialArtsKnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryBlockedCounter += 1;
        }

    }

    private void OnRemoveKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var wearer = args.Wearer;
        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        foreach (var skill in ent.Comp.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, skill.Key) is { } knowledgeUnitActual && TryComp<KnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
            {
                if (knowledgeComponent.Level <= 0)
                    TryRemoveKnowledgeUnit(wearer, skill.Key);
                if (knowledgeComponent.TemporaryLevel - skill.Value < 0)
                    knowledgeComponent.TemporaryLevel = 0;
                else
                    knowledgeComponent.TemporaryLevel -= skill.Value;
            }
        }
        foreach (var experience in ent.Comp.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, experience.Key) is { } knowledgeUnit && TryComp<KnowledgeComponent>(knowledgeUnit, out var knowledgeComponent))
            {
                if (knowledgeComponent.Level <= 0)
                    TryRemoveKnowledgeUnit(wearer, experience.Key);
                else
                    knowledgeComponent.BonusExperience -= experience.Value;
            }
        }
        foreach (var blocked in ent.Comp.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, blocked.Key) is { } knowledgeUnitActual && TryComp<MartialArtsKnowledgeComponent>(knowledgeUnitActual, out var knowledgeComponent))
                knowledgeComponent.TemporaryBlockedCounter -= 1;
        }
    }
}
