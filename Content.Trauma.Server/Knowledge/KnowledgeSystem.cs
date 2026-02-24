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
        SubscribeLocalEvent<KnowledgeConstructionModifierComponent, AfterConstructionChangeEntityEvent>(AlterName);
    }


    private void TransferKnowledge(Entity<KnowledgeHolderComponent> ent, ref KnowledgeCopyEvent args)
    {
        var target = args.Target;
        if (target is not { } || !TryComp<KnowledgeHolderComponent>(target, out var knowledgeHolder))
            return;

        if (TryGetAllKnowledgeUnits(ent) is not { } found)
            return;

        if (TryGetKnowledgeContainer((target.Value, knowledgeHolder)) is not { } targetContainer)
            return;

        if (targetContainer.Comp.KnowledgeContainer == null)
            return;

        foreach (var knowledgeEnt in found)
        {
            _container.Insert(knowledgeEnt.Owner, targetContainer.Comp.KnowledgeContainer);
        }
        ClearKnowledge(ent, false);
    }

    private void AlterName(Entity<KnowledgeConstructionModifierComponent> ent, ref AfterConstructionChangeEntityEvent args)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }
}
