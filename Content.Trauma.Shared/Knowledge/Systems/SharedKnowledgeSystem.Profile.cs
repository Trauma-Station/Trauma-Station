// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Attribute.Attribute;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Provides API for working with <see cref="KnowledgeProfile"/>.
/// </summary>
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private AttributeSystem _attribute = default!;

    private List<EntProtoId> _invalid = new();

    public override void EnsureProfileValid([ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, ref KnowledgeProfile profile)
    {
        var parent = _proto.Index(parentId);

        _invalid.Clear();
        foreach (var (id, mastery) in profile.SkillRolls)
        {
            // remove any masteries that go out of bounds when added to the parent, or if their skill is invalid/cant be bought
            var net = mastery + parent.Profile.SkillRolls.GetValueOrDefault(id);
            if (net < 0 || SkillCost(id, net) == null)
                _invalid.Add(id);
        }

        foreach (var id in _invalid)
        {
            profile.SkillRolls.Remove(id);
        }
    }

    public override void ApplyProfile(EntityUid target, [ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, KnowledgeProfile profile)
    {
        if (GetContainer(target) is not { } ent)
            return;

        var parent = _proto.Index(parentId);
        ApplyProfile(ent, parent.Profile); // species skills first, can't be removed
        ApplyProfile(ent, profile, parent.PointsLimit); // then your extra skills, limited by species points limit
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, not using points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile)
    {
        foreach (var (id, rolls) in profile.SkillRolls)
        {
            if (RaiseSkillByRolls(ent, id, rolls, popup: false) == null)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} skill {id}!");
                continue;
            }
        }
        foreach (var (id, purchases) in profile.Attributes)
        {
            if (EnsureKnowledge<AttributeComponent>(ent, id, 10, false) is not { } unit)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} attribute {id}!");
                continue;
            }

            _attribute.AdjustAttribute(unit, purchases);
        }
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, using limited points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile, int points)
    {
        foreach (var (id, rolls) in profile.SkillRolls)
        {
            if (SkillCost(id, rolls) is not { } cost || points < cost)
                return; // were done here, outdated profile in DB

            if (RaiseSkillByRolls(ent, id, rolls, popup: false) == null)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} skill {id}!");
                continue;
            }

            points -= cost;
        }
        foreach (var (id, purchases) in profile.Attributes)
        {
            if (EnsureKnowledge<AttributeComponent>(ent, id, 10, false) is not { } unit)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} attribute {id}!");
                continue;
            }

            _attribute.AdjustAttribute(unit, purchases);
            // TODO: Figure out point shit.
        }
    }

    public override int ProfileCost(KnowledgeProfile profile)
    {
        var total = 0;
        foreach (var (id, rolls) in profile.SkillRolls)
        {
            total += SkillCost(id, rolls) ?? 0; // this should never have locked skills so ignore if it happens
        }
        foreach (var (id, purchases) in profile.Attributes)
        {
            total += purchases;
        }
        return total;
    }

    /// <summary>
    /// Gets the costs to have a skill at each allowed mastery level.
    /// Returns null if the skill cannot be picked.
    /// </summary>
    public int? SkillCosts(EntProtoId id)
        => AllSkills.TryGetValue(id, out var comp) && comp.Cost is { } cost
            ? cost
            : null;

    /// <summary>
    /// Gets the cost to have a skill at a given mastery level.
    /// Returns null if the skill cannot be picked or the mastery is invalid.
    /// </summary>
    public int? SkillCost(EntProtoId id, int rolls)
        => SkillCosts(id) is { } cost && rolls >= 0
            ? cost * rolls
            : null;
}
