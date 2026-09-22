// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [SubscribeLocalEvent]
    private void OnGeneAdded(Entity<MartialArtsKnowledgeComponent> ent, ref MutationAddedEvent args)
    {
        if (GetContainer(args.Target) is not { } brain)
            return;
        UpdateGeneticBlacklists(brain, args.Target);
    }

    [SubscribeLocalEvent]
    private void OnGeneRemoved(Entity<MartialArtsKnowledgeComponent> ent, ref MutationRemovedEvent args)
    {
        if (GetContainer(args.Target) is not { } brain)
            return;
        UpdateGeneticBlacklists(brain, args.Target);
    }

    /// <summary>
    /// Handles application of genetics blacklists
    /// </summary>
    private void UpdateGeneticBlacklists(Entity<KnowledgeContainerComponent> brain, EntityUid ent)
    {
        // Handles Gene Blacklists
        TryComp<MutatableComponent>(ent, out var mutatable);
        if (GetKnowledgeWith<MartialArtsKnowledgeComponent>(brain) != null && mutatable != null)
        {
            foreach (var (id, martial, y) in GetKnowledgeWith<MartialArtsKnowledgeComponent>(brain)!)
            {
                bool martialsBlocked = false;
                foreach (var mutation in mutatable.Mutations.Keys)
                {
                    if (martial.GeneBlacklist.Contains(mutation))
                        martialsBlocked = true;
                }

                if (martialsBlocked)
                    martial.Blocked = true;
                else
                    martial.Blocked = martial.TemporaryBlockedCounter == 0;
            }
        }
    }
}
