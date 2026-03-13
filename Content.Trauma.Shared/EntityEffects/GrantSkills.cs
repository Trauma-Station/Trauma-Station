// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

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

public sealed class GrantSkillsEffectSystem : EntityEffectSystem<KnowledgeHolderComponent, GrantSkills>
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    protected override void Effect(Entity<KnowledgeHolderComponent> ent, ref EntityEffectEvent<GrantSkills> args)
    {
        _knowledge.AddKnowledgeUnits(ent, args.Effect.Skills);
    }
}
