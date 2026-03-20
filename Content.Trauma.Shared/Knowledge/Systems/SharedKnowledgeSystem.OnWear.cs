// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.EntityConditions;
using Content.Trauma.Common.Silicons.Borgs;
using Content.Trauma.Shared.Knowledge.Components;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;

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
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, BrainInsertedIntoBorgEvent>(OnBrainInsertedIntoBorg);
        SubscribeLocalEvent<KnowledgeGrantOnWearComponent, BrainRemovedFromBorgEvent>(OnBrainRemovedFromBorg);

        SubscribeLocalEvent<ModifyKnowledgeGrantComponent, MapInitEvent>(OnModifyGrantMapInit,
            after: [ typeof(InitialBodySystem) ]); // TODO: move this to a partial of KnowledgeGrantSystem bruh...
    }

    private void OnGrantKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotInsertedEvent args)
        => ApplyKnowledgeModifiers(args.Target, ent);

    private void OnRemoveKnowledgeOrgan(Entity<KnowledgeGrantOnWearComponent> ent, ref OrganGotRemovedEvent args)
        => RemoveKnowledgeModifiers(args.Target, ent);

    private void OnGrantKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotEquippedEvent args)
        => ApplyKnowledgeModifiers(args.Wearer, ent);

    private void OnRemoveKnowledgeWear(Entity<KnowledgeGrantOnWearComponent> ent, ref ClothingGotUnequippedEvent args)
        => RemoveKnowledgeModifiers(args.Wearer, ent);

    private void OnBrainInsertedIntoBorg(Entity<KnowledgeGrantOnWearComponent> ent, ref BrainInsertedIntoBorgEvent args)
        => ApplyKnowledgeModifiers(args.Brain, ent);

    private void OnBrainRemovedFromBorg(Entity<KnowledgeGrantOnWearComponent> ent, ref BrainRemovedFromBorgEvent args)
        => RemoveKnowledgeModifiers(args.Brain, ent);

    private void OnModifyGrantMapInit(Entity<ModifyKnowledgeGrantComponent> ent, ref MapInitEvent args)
    {
        AddGrantedSkills(ent.Owner, user: ent.Owner, ent.Comp.Skills);
        RemComp(ent, ent.Comp);
    }

    private void ApplyKnowledgeModifiers(EntityUid wearer, Entity<KnowledgeGrantOnWearComponent> ent)
    {
        ent.Comp.Applied = _conditions.TryConditions(wearer, ent.Comp.Conditions);
        DirtyField(ent, ent.Comp, nameof(KnowledgeGrantOnWearComponent.Applied));
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
        DirtyField(ent, ent.Comp, nameof(KnowledgeGrantOnWearComponent.Applied));

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

    /// <summary>
    /// One-time adjustment to skills.
    /// Stores the new skills but also adds them to the current user.
    /// </summary>
    public void AddGrantedSkills(Entity<KnowledgeGrantOnWearComponent?> ent, EntityUid user, Dictionary<EntProtoId, int> skills)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        foreach (var (id, level) in skills)
        {
            ent.Comp.Skills[id] = ent.Comp.Skills.GetValueOrDefault(id) + level;
        }
        DirtyField(ent, ent.Comp, nameof(KnowledgeGrantOnWearComponent.Skills));

        // adjust immediately if it was applied already, if it wasn't applied it will be handled later
        if (!ent.Comp.Applied || GetContainer(user) is not { } brain)
            return;

        foreach (var (id, level) in skills)
        {
            if (EnsureKnowledge(brain, id) is { } unit)
            {
                unit.Comp.TemporaryLevel += level;
                Dirty(unit);
            }
        }
    }
}
