// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

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
    /// Each skill and the mastery level from [0, 5]
    /// </summary>
    public Dictionary<EntProtoId, int> Mastery;
    /// <summary>
    /// Skills to remove from the parent profile.
    /// </summary>
    public HashSet<EntProtoId> Removed;

    public KnowledgeProfile(Dictionary<EntProtoId, int> mastery, HashSet<EntProtoId> removed)
    {
        Mastery = mastery;
        Removed = removed;
    }

    /// <summary>
    /// Create an empty profile which uses the parent as-is.
    /// </summary>
    public KnowledgeProfile()
        : this(new(), new())
    {
    }

    /// <summary>
    /// Make a deep copy of another profile
    /// </summary>
    public KnowledgeProfile(KnowledgeProfile other)
        : this(new(other.Mastery), new(other.Removed))
    {
    }

    /// <summary>
    /// Add this profile to a parent profile.
    /// </summary>
    public KnowledgeProfile AddProfile(KnowledgeProfile parent)
    {
        var sum = new KnowledgeProfile(parent);
        foreach (var (id, change) in Mastery)
        {
            sum.Mastery[id] = sum.Mastery.GetValueOrDefault(id) + change;
        }
        foreach (var id in Removed)
        {
            sum.Mastery.Remove(id);
        }
        return sum;
    }

    /// <summary>
    /// Verify potentially outdated/untrusted profile data.
    /// </summary>
    public static KnowledgeProfile Verify(Dictionary<string, int> mastery, List<string> removed, IPrototypeManager proto)
    {
        var profile = new KnowledgeProfile();
        foreach (var (id, change) in mastery)
        {
            // let's hope nobody ever changes a knowledge prototype to become non-knowledge...
            if (!proto.HasIndex(id))
                continue;

            profile.Mastery[id] = change;
        }
        foreach (var id in removed)
        {
            if (proto.HasIndex(id))
                profile.Removed.Add(id);
        }
        return profile;
    }

    public List<string> RemovedList()
    {
        var list = new List<string>(Removed.Count);
        foreach (var id in Removed)
        {
            list.Add(id);
        }
        return list;
    }

    public bool MemberwiseEquals(KnowledgeProfile other)
    {
        if (Mastery.Count != other.Mastery.Count)
            return false;

        foreach (var (id, change) in Mastery)
        {
            if (!other.Mastery.TryGetValue(id, out var otherChange) || otherChange != change)
                return false;
        }

        return true;
    }
}
