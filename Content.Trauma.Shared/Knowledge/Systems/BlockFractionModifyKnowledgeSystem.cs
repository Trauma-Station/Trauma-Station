// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class BlockFractionModifyKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetBlockFractionEvent>(_knowledge.RelayActiveEvent);
    }

    [SubscribeLocalEvent]
    private void OnBlockFractionModify(Entity<BlockFractionKnowledgeComponent> ent, ref GetBlockFractionEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Fraction *= ent.Comp.Curve.GetCurve(level);
    }
}
