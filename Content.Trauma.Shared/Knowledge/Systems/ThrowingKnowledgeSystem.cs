// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Containers;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed class ThrowingKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ModifyThrowInsertChanceEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<ThrowInsertKnowledgeComponent, ModifyThrowInsertChanceEvent>(OnModifyThrowInsertChance);
    }

    private void OnModifyThrowInsertChance(Entity<ThrowInsertKnowledgeComponent> ent, ref ModifyThrowInsertChanceEvent args)
    {
        var level = _knowledge.GetLevel(ent);
        args.Chance += ent.Comp.Curve.GetCurve(level);
    }
}
