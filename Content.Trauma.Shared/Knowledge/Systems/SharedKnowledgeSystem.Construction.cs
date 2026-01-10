using Content.Shared.Body.Components;
using Content.Shared.Construction;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    public void OnConstructionGetGroupEventBodyPart(Entity<BodyComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        foreach (var part in _body.GetBodyOrgans(ent))
        {
            if (TryComp<KnowledgeContainerComponent>(part.Id, out var knowledgeContainer))
            {
                RaiseLocalEvent(part.Id, ref args);
                return;
            }
        }
    }

    public void OnConstructionGetGroupEvent(Entity<KnowledgeContainerComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        if (TryGetKnowledgeWithComp<ConstructionKnowledgeComponent>(ent) is not { } knowledge)
            return;

        foreach (var (_, comp, _) in knowledge)
        {
            args.Groups.Add(comp.Group);
        }
    }
}
