// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    public void InitializeMelee()
    {
        SubscribeLocalEvent<KnowledgeHolderComponent, MissAttackEvent>(OnMissAttack);
    }

    private void OnMissAttack(Entity<KnowledgeHolderComponent> ent, ref MissAttackEvent args)
    {
        var knowledgeMiss = 1.0f;
        if (TryGetKnowledgeUnit(ent, MeleeKnowledge) is { } melee)
        {
            if (GetMastery(melee) < 2)
            {
                knowledgeMiss = ((float) melee.Comp.Level + args.Adjust) / 26.0f;
            }
        }

        if (knowledgeMiss < 1.0f && !SharedRandomExtensions.PredictedProb(_timing, Math.Max(1.0f - knowledgeMiss, 0), GetNetEntity(ent)))
            return;

        args.Miss = true;
    }
}
