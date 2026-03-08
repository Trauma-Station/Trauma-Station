// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Cloning;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Knowledge;

public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, TransferredToCloneEvent>(TransferKnowledge);
    }

    // TODO: move to shared bruh
    /// <summary>
    /// Attempts to transfer all knowledge from the original entity into the cloned mob.
    /// </summary>
    private void TransferKnowledge(Entity<KnowledgeHolderComponent> ent, ref TransferredToCloneEvent args)
    {
        if (TryGetAllKnowledgeUnits(ent) is not { } found)
            return;

        var mob = args.Cloned;
        var holder = EnsureComp<KnowledgeHolderComponent>(mob);
        var mobContainer = EnsureKnowledgeContainer((mob, holder));
        if (mobContainer.Comp.Container is not { } container)
            return;

        foreach (var knowledgeEnt in found)
        {
            _container.Insert(knowledgeEnt.Owner, container);
        }
        ClearKnowledge(ent, false);
    }
}
