using Content.Shared.Construction;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    public void OnConstructionGetGroupEvent(Entity<KnowledgeContainerComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        if (TryGetKnowledgeWithComp<KnowledgeComponent>(ent) is not { } knowledge)
            return;

        foreach (var entity in knowledge)
        {
            var meta = MetaData(entity);

            if (meta.EntityPrototype == null)
                continue;

            var protoId = meta.EntityPrototype.ID;

            if (TryComp<KnowledgeComponent>(entity, out var comp))
                args.Groups.Add(protoId, comp.Level);
        }
    }
}
