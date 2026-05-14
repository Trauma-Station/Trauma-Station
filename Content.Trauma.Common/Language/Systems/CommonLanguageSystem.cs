// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language.Components;

namespace Content.Trauma.Common.Language.Systems;

public abstract partial class CommonLanguageSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    /// <summary>
    /// A cached instance of <see cref="PsychomanticPrototype"/>.
    /// </summary>
    public static LanguagePrototype Psychomantic { get; private set; } = default!;

    /// <summary>
    ///     A cached instance of <see cref="UniversalPrototype"/>
    /// </summary>
    public static LanguagePrototype Universal { get; private set; } = default!;

    public override void Initialize()
    {
        base.Initialize();


        Universal = _prototype.Index<LanguagePrototype>(UniversalPrototype);
        Psychomantic = _prototype.Index<LanguagePrototype>(PsychomanticPrototype);
    }

    /// <summary>
    ///     The language used as a fallback in cases where an entity suddenly becomes a Language Speaker (e.g. the usage of make-sentient).
    /// </summary>
    public static readonly ProtoId<LanguagePrototype> FallbackLanguagePrototype = "TauCetiBasic";

    /// <summary>
    ///     The language whose speakers are assumed to understand and speak every language. Should never be added directly.
    /// </summary>
    public static readonly ProtoId<LanguagePrototype> UniversalPrototype = "Universal";

    /// <summary>
    ///     Language used for Xenoglossy, should have same effects as Universal but with different language prototype.
    /// </summary>
    public static readonly ProtoId<LanguagePrototype> PsychomanticPrototype = "Psychomantic";

    /// <summary>
    ///     Generates a stable pseudo-random number in the range (min, max) (inclusively) for the given seed.
    ///     One seed always corresponds to one number, however the resulting number also depends on the current round number.
    ///     This method is meant to be used in <see cref="ObfuscationMethod"/> to provide stable obfuscation.
    /// </summary>
    public abstract int PseudoRandomNumber(int seed, int min, int max);

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public abstract LanguagePrototype GetLanguage(Entity<LanguageSpeakerComponent?> ent);

    /// <summary>
    ///     Adds a new language to the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public abstract void AddLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language, bool addSpoken = true, bool addUnderstood = true);

    /// <summary>
    ///     Removes a language from the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public abstract void RemoveLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language, bool removeSpoken = true, bool removeUnderstood = true);

    /// <summary>
    ///     Obfuscate a message using the given language.
    /// </summary>
    public abstract string ObfuscateSpeech(string message, LanguagePrototype language, EntityUid messageSource);

    public abstract bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language);

    /// <summary>
    ///     Immediately refreshes the cached lists of spoken and understood languages for the given entity.
    /// </summary>
    public abstract void UpdateEntityLanguages(Entity<LanguageSpeakerComponent?> ent);
}
