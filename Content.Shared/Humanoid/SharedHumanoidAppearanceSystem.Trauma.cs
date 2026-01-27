using Content.Goobstation.Common.Barks;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - barks and methods moved from server
/// </summary>
public abstract partial class SharedHumanoidAppearanceSystem
{
    public static readonly ProtoId<BarkPrototype> DefaultBarkVoice = "Alto";

    public void SetBarkVoice(EntityUid uid, string? barkvoiceId, HumanoidAppearanceComponent humanoid)
    {
        var voicePrototypeId = DefaultBarkVoice;

        if (barkvoiceId != null &&
            _proto.TryIndex<BarkPrototype>(barkvoiceId, out var bark) &&
            bark.SpeciesWhitelist?.Contains(humanoid.Species) != false)
        {
            voicePrototypeId = barkvoiceId;
        }
        else
        {
            // find first valid roundstart bark to use
            foreach (var proto in _proto.EnumeratePrototypes<BarkPrototype>())
            {
                if (proto.RoundStart && proto.SpeciesWhitelist?.Contains(humanoid.Species) != false)
                {
                    voicePrototypeId = proto.ID;
                    break;
                }
            }
        }

        EnsureComp<SpeechSynthesisComponent>(uid, out var comp);
        comp.VoicePrototypeId = voicePrototypeId;
        humanoid.BarkVoice = voicePrototypeId;
        Dirty(uid, comp);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by ID.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="marking">The marking to try and remove.</param>
    /// <param name="sync">Whether to immediately sync this to the humanoid</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, string marking, bool sync = true, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid)
            || !_markingManager.Markings.TryGetValue(marking, out var prototype))
        {
            return;
        }

        humanoid.MarkingSet.Remove(prototype.MarkingCategory, marking);

        if (sync)
            Dirty(uid, humanoid);
    }

    /// <summary>
    ///     Removes a marking from a humanoid by category and index.
    /// </summary>
    /// <param name="uid">Humanoid mob's UID</param>
    /// <param name="category">Category of the marking</param>
    /// <param name="index">Index of the marking</param>
    /// <param name="humanoid">Humanoid component of the entity</param>
    public void RemoveMarking(EntityUid uid, MarkingCategories category, int index, HumanoidAppearanceComponent? humanoid = null)
    {
        if (index < 0
            || !Resolve(uid, ref humanoid)
            || !humanoid.MarkingSet.TryGetCategory(category, out var markings)
            || index >= markings.Count)
        {
            return;
        }

        humanoid.MarkingSet.Remove(category, index);
        Dirty(uid, humanoid);
    }
}
