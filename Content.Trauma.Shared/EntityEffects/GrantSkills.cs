// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Grants minimum skill levels to the target mob.
/// </summary>
public sealed partial class GrantSkills : EntityEffectBase<GrantSkills>
{
    /// <summary>
    /// Each skill and the minimum level to ensure the target has.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class GrantSkillsEffectSystem : EntityEffectSystem<MetaDataComponent, GrantSkills>
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<GrantSkills> args)
    {
        _knowledge.EnsureKnowledgeContainer(ent);
        _knowledge.AddKnowledgeUnits(ent, args.Effect.Skills);
    }
}
