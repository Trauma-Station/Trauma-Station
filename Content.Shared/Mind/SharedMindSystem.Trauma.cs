using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Systems;

namespace Content.Shared.Mind;

/// <summary>
/// Trauma - language related mind additions.
/// </summary>
public abstract partial class SharedMindSystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!; // Trauma

    // TODO: make it only delete certain objectives and not all of them in case an antag is ever converted and then deconverted.
    public void ClearObjectives(Entity<MindComponent?> mind)
    {
        if (!Resolve(mind, ref mind.Comp))
            return;

        foreach (var obj in mind.Comp.Objectives)
        {
            QueueDel(obj);
        }
        mind.Comp.Objectives.Clear();
        Dirty(mind, mind.Comp);
    }

    public void EnsureDefaultLanguage(EntityUid uid)
    {
        var speaker = EnsureComp<LanguageSpeakerComponent>(uid);

        // If the entity already speaks some language (like monkey or robot), we do nothing else.
        // Otherwise, we give them the fallback language
        if (speaker.SpokenLanguages.Count == 0)
            _language.AddLanguage(uid, SharedLanguageSystem.FallbackLanguagePrototype);
    }
}
