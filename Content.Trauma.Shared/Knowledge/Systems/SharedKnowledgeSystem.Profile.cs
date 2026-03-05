// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Provides API for working with <see cref="KnowledgeProfile"/>.
/// </summary>
public abstract partial class SharedKnowledgeSystem
{
    private List<EntProtoId> _invalid = new();

    /// <summary>
    /// Sanitize a profile, removing any invalid skills.
    /// Does not care about point limits.
    /// </summary>
    public void SanitizeProfile(ref KnowledgeProfile profile)
    {
        _invalid.Clear();
        foreach (var (id, mastery) in profile.Mastery)
        {
            if (!AllKnowledges.ContainsKey(id) || mastery < 0 || mastery > 5)
                _invalid.Add(id);
        }

        foreach (var id in _invalid)
        {
            profile.Mastery.Remove(id);
        }
    }

    public override void ApplyProfile(EntityUid target, [ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, KnowledgeProfile profile)
    {
        if (GetContainer(target) is not {} ent)
            return;

        var parent = _proto.Index(parentId);
        ApplyProfile(ent, profile.AddProfile(parent.Profile), parent.Points);
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, using limited points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile, int points)
    {
        foreach (var (id, mastery) in profile.Mastery)
        {
            var cost = SkillCost(id, mastery);
            if (points < cost)
                return; // were done here, outdated profile in DB

            var level = GetInverseMastery(mastery);
            if (EnsureKnowledge(ent, id, level) == null)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} knowledge {id}!");
                continue;
            }

            points -= cost;
        }
    }

    public override int ProfileCost(KnowledgeProfile profile)
    {
        var total = 0;
        foreach (var (id, mastery) in profile.Mastery)
        {
            total += SkillCost(id, mastery);
        }
        return total;
    }

    /// <summary>
    /// Gets the cost to have a skill at a given mastery level.
    /// Throws for invalid skills or mastery values.
    /// </summary>
    public int SkillCost(EntProtoId id, int mastery)
        => AllKnowledges[id].Costs[mastery];
}
