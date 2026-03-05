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
public partial record struct KnowledgeProfile(Dictionary<EntProtoId, int> Mastery)
{
    /// <summary>
    /// Rust Clone trait in c# real
    /// </summary>
    public KnowledgeProfile Copy()
        => new(new(Mastery));

    /// <summary>
    /// Add this profile to a parent profile.
    /// </summary>
    public KnowledgeProfile AddProfile(KnowledgeProfile parent)
    {
        var sum = parent.Copy();
        foreach (var (id, change) in Mastery)
        {
            sum.Mastery[id] = sum.Mastery.GetValueOrDefault(id) + change;
        }
        return sum;
    }

    public static KnowledgeProfile Verify(Dictionary<string, int> mastery, IPrototypeManager proto)
    {
        var profile = new KnowledgeProfile(new());
        foreach (var (id, change) in mastery)
        {
            // let's hope nobody ever changes a knowledge prototype to become non-knowledge...
            if (!proto.HasIndex(id))
                continue;

            profile.Mastery[id] = change;
        }
        return profile;
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
