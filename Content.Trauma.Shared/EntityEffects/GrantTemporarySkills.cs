// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
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
    GrantTemporarySkillsEffectSystem : EntityEffectSystem<MetaDataComponent, GrantTemporarySkills>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<GrantTemporarySkills> args)
    {
        var brain = _knowledge.EnsureKnowledgeContainer(ent);

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
