// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.EntityConditions;
using Content.Trauma.Shared.Knowledge.Components;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly SharedEntityConditionsSystem _conditions = default!;

    private void InitializeOnWear()
    {
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, OrganGotInsertedEvent>(OnGrantKnowledgeOrgan);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, OrganGotRemovedEvent>(OnRemoveKnowledgeOrgan);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotEquippedEvent>(OnGrantKnowledgeWear);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, ClothingGotUnequippedEvent>(OnRemoveKnowledgeWear);
    }

    private void OnGrantKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotInsertedEvent args)
        => ApplyKnowledgeModifiers(args.Target, ent);

    private void OnRemoveKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotRemovedEvent args)
        => RemoveKnowledgeModifiers(args.Target, ent);

    private void OnGrantKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotEquippedEvent args)
        => ApplyKnowledgeModifiers(args.Wearer, ent);

    private void OnRemoveKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotUnequippedEvent args)
        => RemoveKnowledgeModifiers(args.Wearer, ent);

    private void ApplyKnowledgeModifiers(EntityUid wearer, Entity<KnowledgeGrantOnWearComponent> ent)
    {
        ent.Comp.Applied = _conditions.TryConditions(wearer, ent.Comp.Conditions);
        Dirty(ent);
        if (!ent.Comp.Applied || GetContainer(wearer) is not { } brain)
            return;

        // Handle Skills (Temporary Levels)
        foreach (var (id, level) in ent.Comp.Skills)
        {
            if (EnsureKnowledge(brain, id) is { } unit)
            {
                unit.Comp.TemporaryLevel += level;
                Dirty(unit);
            }
        }

        // Handle Experience
        // FIXME: it should be a separate thing since this gives you the skill for free
        /*foreach (var (id, xp) in ent.Comp.Experience)
        {
            if (GetKnowledge(brain, id) is {} unit)
            {
                unit.Comp.BonusExperience += xp;
                Dirty(unit);
            }
        }*/

        // Handle Blocks
        foreach (var id in ent.Comp.Blocked.Keys)
        {
            if (GetKnowledge(brain, id) is { } unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
            {
                martial.TemporaryBlockedCounter++;
                martial.Blocked = true;
                Dirty(unit, martial);
            }
        }
    }

    private void RemoveKnowledgeModifiers(EntityUid wearer, Entity<KnowledgeGrantOnWearComponent> ent)
    {
        if (TerminatingOrDeleted(wearer) || !ent.Comp.Applied || GetContainer(wearer) is not { } brain)
            return;

        ent.Comp.Applied = false;
        Dirty(ent);

        // Remove Skills
        foreach (var (id, level) in ent.Comp.Skills)
        {
            if (GetKnowledge(brain, id) is not { } unit)
                continue;

            unit.Comp.TemporaryLevel = Math.Max(0, unit.Comp.TemporaryLevel - level);

            // If they have no real levels and no more temp levels, clean up
            if (unit.Comp.NetLevel <= 0)
                RemoveKnowledge(brain, id);
            else
                Dirty(unit);
        }

        // Remove Experience
        /*foreach (var (id, xp) in ent.Comp.Experience)
        {
            if (GetKnowledge(brain, id) is not {} unit)
                continue;

            unit.Comp.BonusExperience -= xp;

            if (unit.Comp.Level <= 0 && unit.Comp.BonusExperience <= 0)
                RemoveKnowledge(brain, id);
            else
                Dirty(unit);
        }*/

        // Remove Blocks
        foreach (var id in ent.Comp.Blocked.Keys)
        {
            if (GetKnowledge(brain, id) is { } unit && TryComp<MartialArtsKnowledgeComponent>(unit, out var martial))
            {
                martial.Blocked = --martial.TemporaryBlockedCounter == 0;
                Dirty(unit, martial);
            }
        }
    }
}
