// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    private void InitializeConstruction()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, ConstructionGetGroupsEvent>(OnConstructionGetGroupEvent);
    }

    public void OnConstructionGetGroupEvent(Entity<KnowledgeHolderComponent> ent, ref ConstructionGetGroupsEvent args)
    {
        if (TryGetAllKnowledgeUnits(ent) is not { } knowledge)
            return;

        foreach (var entity in knowledge)
        {
            if (Prototype(entity)?.ID is { } protoId && TryComp<KnowledgeComponent>(entity, out var comp))
                args.Groups.Add(protoId, comp.Level);
        }
    }
}
