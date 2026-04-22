// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.MartialArts;

namespace Content.Trauma.Common.Knowledge.Systems;

public abstract partial class CommonKnowledgeSystem : EntitySystem
{
    /// <summary>
    /// Gets a skill unit based on its entity prototype ID.
    /// </summary>
    public abstract Entity<SkillComponent>? GetSkill(EntityUid target, [ForbidLiteral] EntProtoId knowledgeUnit);


    /// <summary>
    /// Gets an attribute unit based on its entity prototype ID.
    /// </summary>
    public abstract Entity<AttributeComponent>? GetAttribute(EntityUid target, [ForbidLiteral] EntProtoId knowledgeUnit);

    /// <summary>
    /// Clears a knowledge from the target entity.
    /// </summary>
    public abstract void ClearKnowledge(EntityUid target, bool deleteAll);

    /// <summary>
    /// Get every skill and the mastery level of a mob.
    /// </summary>
    public abstract Dictionary<EntProtoId, int> GetSkillMasteries(EntityUid target);

    /// <summary>
    /// Gets the mastery level for a skill level.
    /// </summary>
    public abstract int GetMastery(int level);

    public int GetMastery(SkillComponent comp)
        => GetMastery(comp.NetLevel);

    /// <summary>
    /// Get the name for a given mastery number.
    /// Clamps the number if its out of bounds.
    /// </summary>
    public abstract string GetMasteryString(int level);

    /// <summary>
    /// Gets the mastery level of a skill unit's entity.
    /// </summary>
    public abstract int GetMastery(EntityUid uid);

    /// <summary>
    ///Gets the mastery level from a skill category.
    /// </summary>
    public abstract int GetInverseMastery(int number);

    /// <summary>
    /// Curve scale that determines some functionality. Goes from 0 to 1.
    /// </summary>
    public abstract float SharpCurve(Entity<SkillComponent> knowledge, int offset = 0, float inverseScale = 100.0f);

    /// <summary>
    /// Sanitize a profile, removing any invalid skills.
    /// Does not care about point limits.
    /// </summary>
    public abstract void EnsureProfileValid([ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, ref KnowledgeProfile profile);

    /// <summary>
    /// Apply a parent and character profile to a mob.
    /// This clears the knowledge container then adds every skill allowed by the parent's points.
    /// </summary>
    public abstract void ApplyProfile(EntityUid target, [ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, KnowledgeProfile profile);

    /// <summary>
    /// Gets the total point cost for every skill in a profile.
    /// </summary>
    public abstract int ProfileCost(KnowledgeProfile profile);
}
