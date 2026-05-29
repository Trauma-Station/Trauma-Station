// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Grants temporary skills to target.
/// </summary>
public sealed partial class GrantTemporarySkills : EntityEffectBase<GrantTemporarySkills>
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();
}

public sealed partial class
    GrantTemporarySkillsEffectSystem : EntityEffectSystem<KnowledgeHolderComponent, GrantTemporarySkills>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<KnowledgeHolderComponent> ent, ref EntityEffectEvent<GrantTemporarySkills> args)
    {
        if (_knowledge.GetContainer(ent) is not { } brain)
            return;

        foreach (var (id, level) in args.Effect.Skills)
        {
            if (_knowledge.EnsureKnowledge(brain, id) is { } unit)
            {
                unit.Comp.TemporaryLevel += level;
                Dirty(unit);
            }
        }
    }
}
