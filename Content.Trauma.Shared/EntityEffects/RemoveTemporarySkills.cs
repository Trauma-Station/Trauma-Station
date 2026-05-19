// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes temporary skills to target.
/// </summary>
public sealed partial class RemoveTemporarySkills : EntityEffectBase<RemoveTemporarySkills>
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();
}

public sealed partial class
    RemoveTemporarySkillsEffectSystem : EntityEffectSystem<KnowledgeHolderComponent, RemoveTemporarySkills>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<KnowledgeHolderComponent> ent, ref EntityEffectEvent<RemoveTemporarySkills> args)
    {
        if (_knowledge.GetContainer(ent) is not { } brain)
            return;

        foreach (var (id, level) in args.Effect.Skills)
        {
            if (_knowledge.GetKnowledge(brain, id) is not { } unit)
                continue;

            unit.Comp.TemporaryLevel = Math.Max(0, unit.Comp.TemporaryLevel - level);

            // If they have no real levels and no more temp levels, clean up
            if (unit.Comp.NetLevel <= 0)
                _knowledge.RemoveKnowledge(brain, id);
            else
                Dirty(unit);
        }
    }
}
