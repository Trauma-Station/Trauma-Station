// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Genetics.Mutations;

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
