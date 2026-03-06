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
        if (TryGetAllKnowledgeUnits(ent) is not {} knowledge)
            return;

        foreach (var unit in knowledge)
        {
            if (Prototype(unit)?.ID is { } protoId)
                args.Groups.Add(protoId, unit.Comp.Level);
            else
                Log.Error($"Non-prototyped knowledge entity {ToPrettyString(unit)} inside {ToPrettyString(ent)}");
        }
    }
}
