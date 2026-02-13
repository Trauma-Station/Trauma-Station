using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Content.Trauma.Shared.MartialArts.Events;

namespace Content.Trauma.Client.Knowledge;
public sealed class KnowledgeSystem : SharedKnowledgeSystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, OpenMartialArtsMenuEvent>(OnOpenMartialArtsMenu);
        SubscribeLocalEvent<KnowledgeHolderComponent, GetPerformedAttackTypesEvent>(OnGetAttackTypes);
    }

    private void OnGetAttackTypes(Entity<KnowledgeHolderComponent> ent, ref GetPerformedAttackTypesEvent args)
    {
        if (ent.Comp.KnowledgeEntity is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainerComp))
            return;

        if (knowledgeContainerComp.MartialArtSkillUid is not { } martialArtSkillUid || !TryComp<CanPerformComboComponent>(martialArtSkillUid, out var comboComp))
            return;

        args.AttackTypes = comboComp.LastAttacks;
    }
}
