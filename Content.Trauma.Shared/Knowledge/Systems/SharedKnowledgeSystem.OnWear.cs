// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Clothing;
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
        if (GetContainer(wearer) is not {} ent)
            return;

        // Handle Skills (Temporary Levels)
        foreach (var (id, level) in component.Skills)
        {
            if (EnsureKnowledge(ent, id) is {} unit)
            {
                unit.Comp.TemporaryLevel += level;
                Dirty(unit);
            }
        }

        // Handle Experience
        // FIXME: it should be a separate thing since this gives you the skill for free
        /*foreach (var (id, xp) in component.Experience)
        {
            if (GetKnowledge(ent, id) is {} unit)
            {
                unit.Comp.BonusExperience += xp;
                Dirty(unit);
            }
        }*/

        // Handle Blocks
        foreach (var id in component.Blocked.Keys)
        {
            if (GetKnowledge(ent, id) is {} unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
            {
                martial.TemporaryBlockedCounter += 1;
                Dirty(unit, martial);
            }
        }
    }

    private void RemoveKnowledgeModifiers(EntityUid wearer, KnowledgeGrantOnWearComponent component)
    {
        if (TerminatingOrDeleted(wearer) || GetContainer(wearer) is not {} ent)
            return;

        // Remove Skills
        foreach (var (id, level) in component.Skills)
        {
            if (GetKnowledge(ent, id) is not {} unit)
                continue;

            unit.Comp.TemporaryLevel = Math.Max(0, unit.Comp.TemporaryLevel - level);

            // If they have no real levels and no more temp levels, clean up
            if (unit.Comp.Level <= 0 && unit.Comp.TemporaryLevel <= 0)
                RemoveKnowledge(ent, id);
            else
                Dirty(unit);
        }

        // Remove Experience
        /*foreach (var (id, xp) in component.Experience)
        {
            if (GetKnowledge(ent, id) is not {} unit)
                continue;

            unit.Comp.BonusExperience -= xp;

            if (unit.Comp.Level <= 0 && unit.Comp.BonusExperience <= 0)
                RemoveKnowledge(ent, id);
            else
                Dirty(unit);
        }*/

        // Remove Blocks
        foreach (var id in component.Blocked.Keys)
        {
            if (GetKnowledge(ent, id) is {} unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
            {
                martial.TemporaryBlockedCounter -= 1;
                Dirty(unit, martial);
            }
        }
    }
}
