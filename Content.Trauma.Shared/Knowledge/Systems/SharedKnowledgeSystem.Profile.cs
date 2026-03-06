// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Provides API for working with <see cref="KnowledgeProfile"/>.
/// </summary>
public abstract partial class SharedKnowledgeSystem
{
    public Dictionary<KnowledgeProfilePrototype, int> PointLimits = new();

    private List<EntProtoId> _invalid = new();

    private void LoadProfilePrototypes()
    {
        PointLimits.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<KnowledgeProfilePrototype>())
        {
            PointLimits[proto] = ProfileCost(proto.Profile);
        }
    }

    public override void EnsureProfileValid([ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, ref KnowledgeProfile profile)
    {
        var parent = _proto.Index(parentId);

        _invalid.Clear();
        foreach (var (id, mastery) in profile.Mastery)
        {
            // remove any masteries that go out of bounds when added to the parent, or if their skill is invalid/cant be bought
            var net = mastery + parent.Profile.Mastery.GetValueOrDefault(id);
            if (net < 0 || net > 5 || SkillCost(id, net) == null)
                _invalid.Add(id);
        }

        foreach (var id in _invalid)
        {
            profile.Mastery.Remove(id);
        }
    }

    public override void ApplyProfile(EntityUid target, [ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, KnowledgeProfile profile)
    {
        if (GetContainer(target) is not { } ent)
            return;

        var parent = _proto.Index(parentId);
        ApplyProfile(ent, profile.AddProfile(parent.Profile), PointLimits[parent]);
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, using limited points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile, int points)
    {
        foreach (var (id, mastery) in profile.Mastery)
        {
            if (SkillCost(id, mastery) is not { } cost || points < cost)
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
            total += SkillCost(id, mastery) ?? 0; // this should never have locked skills so ignore if it happens
        }
        return total;
    }

    /// <summary>
    /// Gets the cost to have a skill at a given mastery level.
    /// Returns null if the skill cannot be picked.
    /// Throws for invalid mastery values.
    /// </summary>
    public int? SkillCost(EntProtoId id, int mastery)
        => AllKnowledges.TryGetValue(id, out var comp) && comp.Costs is { } costs
            ? costs[mastery]
            : null;
}
