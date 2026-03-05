// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Common.MartialArts;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Common.Knowledge.Systems;

public abstract partial class CommonKnowledgeSystem : EntitySystem
{
    /// <summary>
    /// Gets a knowledge unit based on its entity prototype ID.
    /// </summary>
    public abstract Entity<KnowledgeComponent>? GetKnowledge(EntityUid target, [ForbidLiteral] EntProtoId knowledgeUnit);

    /// <summary>
    /// Clears Knowledge from the target entity.
    /// </summary>
    public abstract void ClearKnowledge(EntityUid target, bool deleteAll);

    /// <summary>
    /// Gets the mastery level for a knowledge level.
    /// </summary>
    public abstract int GetMastery(int level);

    public int GetMastery(KnowledgeComponent comp)
        => GetMastery(comp.Level);

    /// <summary>
    /// Gets the mastery level of a knowledge unit's entity.
    /// </summary>
    public abstract int GetMastery(EntityUid uid);

    /// <summary>
    ///Gets the mastery level from a category.
    /// </summary>
    public abstract int GetInverseMastery(int number);

    /// <summary>
    /// Curve scale that determines some functionality. Goes from 0 to 1.
    /// </summary>
    public abstract float SharpCurve(Entity<KnowledgeComponent> knowledge, int offset = 0, float inverseScale = 100.0f);

    /// <summary>
    /// Runs quality instructions for an item outside of the construction loop, such as the bullets for the shotgun ammo.
    /// </summary>
    public abstract void ModifyValues(Entity<QualityComponent> ent);
}
