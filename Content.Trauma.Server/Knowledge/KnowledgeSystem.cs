// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Construction;
using Content.Shared.NameModifier.EntitySystems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Server.Knowledge;

public sealed class KnowledgeSystem : SharedKnowledgeSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, KnowledgeCopyEvent>(TransferKnowledge);
        SubscribeLocalEvent<QualityComponent, AfterConstructionChangeEntityEvent>(AlterName);
    }

    /// <summary>
    /// Attempts to transfer all knowledge from the raised entity into a target mob.
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void TransferKnowledge(Entity<KnowledgeHolderComponent> ent, ref KnowledgeCopyEvent args)
    {
        if (args.Target is not { } mob)
            return;

        if (!TryComp<KnowledgeHolderComponent>(mob, out var knowledgeHolder))
            knowledgeHolder = EnsureComp<KnowledgeHolderComponent>(mob);

        if (TryGetAllKnowledgeUnits(ent) is not { } found)
            return;

        var mobContainer = EnsureKnowledgeContainer((mob, knowledgeHolder));

        if (mobContainer.Comp.KnowledgeContainer is not { } container)
            return;

        foreach (var knowledgeEnt in found)
        {
            _container.Insert(knowledgeEnt.Owner, container);
        }
        ClearKnowledge(ent, false);
    }

    private void AlterName(Entity<QualityComponent> ent, ref AfterConstructionChangeEntityEvent args)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }
}
