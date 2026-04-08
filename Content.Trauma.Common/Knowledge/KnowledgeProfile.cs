// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Stores changes to skill masteries for either a species or character.
/// For a species it is used inside <see cref="KnowledgeProfilePrototype"/> and is absolute.
/// For a character it is relative to the species.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct KnowledgeProfile
{
    /// <summary>
    /// Each skill and the amount of rolls this skill will get
    /// </summary>
    public Dictionary<EntProtoId, int> SkillRolls;

    /// <summary>
    /// Each attribute that the character will have.
    /// </summary>
    public Dictionary<EntProtoId, int> Attributes;

    public KnowledgeProfile(Dictionary<EntProtoId, int> attributes, Dictionary<EntProtoId, int> skillRolls)
    {
        Attributes = attributes;
        SkillRolls = skillRolls;
    }

    /// <summary>
    /// Create an empty profile which uses the parent as-is.
    /// </summary>
    public KnowledgeProfile()
        : this(new Dictionary<EntProtoId, int>(), new Dictionary<EntProtoId, int>())
    {
    }

    /// <summary>
    /// Make a deep copy of another profile
    /// </summary>
    public KnowledgeProfile(KnowledgeProfile other)
        : this(new Dictionary<EntProtoId, int>(other.Attributes), new Dictionary<EntProtoId, int>(other.SkillRolls))
    {
    }

    /// <summary>
    /// Verify potentially outdated/untrusted profile data.
    /// </summary>
    public static KnowledgeProfile Verify(Dictionary<string, int> skillRolls, Dictionary<string, int> attributePurchases, IPrototypeManager proto)
    {
        var profile = new KnowledgeProfile();
        foreach (var (id, change) in skillRolls)
        {
            // let's hope nobody ever changes a knowledge prototype to become non-knowledge...
            if (!proto.HasIndex(id))
                continue;

            // skill stuff
            profile.SkillRolls[id] = change;
        }
        foreach (var (id, change) in attributePurchases)
        {
            // let's hope nobody ever changes a knowledge prototype to become non-knowledge...
            if (!proto.HasIndex(id))
                continue;

            // skill stuff
            profile.Attributes[id] = change;
        }
        return profile;
    }

    public bool MemberwiseEquals(KnowledgeProfile other)
    {
        if (SkillRolls.Count != other.SkillRolls.Count || Attributes.Count != other.Attributes.Count)
            return false;

        foreach (var (id, change) in SkillRolls)
        {
            if (!other.SkillRolls.TryGetValue(id, out var otherChange) || otherChange != change)
                return false;
        }

        foreach (var (id, change) in Attributes)
        {
            if (!other.Attributes.TryGetValue(id, out var otherChange) || otherChange != change)
                return false;
        }

        return true;
    }
}
