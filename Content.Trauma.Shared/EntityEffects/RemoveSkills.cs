// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes a list of skills from the target mob.
/// </summary>
public sealed partial class RemoveSkills : EntityEffectBase<RemoveSkills>
{
    /// <summary>
    /// Each skill to remove.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Skills = new();

    [DataField]
    public bool Force;
}

public sealed partial class RemoveSkillsEffectSystem : EntityEffectSystem<KnowledgeHolderComponent, RemoveSkills>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<KnowledgeHolderComponent> ent, ref EntityEffectEvent<RemoveSkills> args)
    {
        var force = args.Effect.Force;
        foreach (var skill in args.Effect.Skills)
        {
            _knowledge.RemoveKnowledge(ent, skill, force);
        }
    }
}
