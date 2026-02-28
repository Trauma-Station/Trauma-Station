// SPDX-License-Identifier: AGPL-3.0-or-later

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
    => ApplyKnowledgeModifiers(args.Target, ent.Comp);

    private void OnRemoveKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotRemovedEvent args)
        => RemoveKnowledgeModifiers(args.Target, ent.Comp);

    private void OnGrantKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotEquippedEvent args)
        => ApplyKnowledgeModifiers(args.Wearer, ent.Comp);

    private void OnRemoveKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotUnequippedEvent args)
        => RemoveKnowledgeModifiers(args.Wearer, ent.Comp);

    private void ApplyKnowledgeModifiers(EntityUid wearer, KnowledgeGrantOnWearComponent component)
    {
        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        // Handle Skills (Temporary Levels)
        foreach (var (id, level) in component.Skills)
        {
            var unit = TryGetKnowledgeUnit(wearer, id) ?? TryAddKnowledgeUnit(wearer, (id, 0));
            if (unit is { } && TryComp<KnowledgeComponent>(unit, out var knowledge))
                knowledge.TemporaryLevel += level;
        }

        // Handle Experience
        foreach (var (id, xp) in component.Experience)
        {
            var unit = TryGetKnowledgeUnit(wearer, id) ?? TryAddKnowledgeUnit(wearer, (id, 0));
            if (unit is { } && TryComp<KnowledgeComponent>(unit, out var knowledge))
                knowledge.BonusExperience += xp;
        }

        // Handle Blocks
        foreach (var (id, _) in component.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, id) is { } unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
                martial.TemporaryBlockedCounter += 1;
        }
    }

    private void RemoveKnowledgeModifiers(EntityUid wearer, KnowledgeGrantOnWearComponent component)
    {
        if (TryGetKnowledgeEntity(wearer) is not { } knowledgeEntity)
            return;

        // Remove Skills
        foreach (var (id, level) in component.Skills)
        {
            if (TryGetKnowledgeUnit(wearer, id) is not { } unit || !TryComp<KnowledgeComponent>(unit, out var knowledge))
                continue;

            knowledge.TemporaryLevel = Math.Max(0, knowledge.TemporaryLevel - level);

            // If they have no real levels and no more temp levels, clean up
            if (knowledge.Level <= 0 && knowledge.TemporaryLevel <= 0)
                TryRemoveKnowledgeUnit(wearer, id);
        }

        // Remove Experience
        foreach (var (id, xp) in component.Experience)
        {
            if (TryGetKnowledgeUnit(wearer, id) is not { } unit || !TryComp<KnowledgeComponent>(unit, out var knowledge))
                continue;

            knowledge.BonusExperience -= xp;

            if (knowledge.Level <= 0 && knowledge.BonusExperience <= 0)
                TryRemoveKnowledgeUnit(wearer, id);
        }

        // Remove Blocks
        foreach (var (id, _) in component.Blocked)
        {
            if (TryGetKnowledgeUnit(wearer, id) is { } unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
                martial.TemporaryBlockedCounter -= 1;
        }
    }
}
