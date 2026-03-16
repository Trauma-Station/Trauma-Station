using Robust.Shared.Prototypes;
using Content.EinsteinEngines.Common.Language.Components;

namespace Content.EinsteinEngines.Common.Language.Systems;

public abstract class CommonLanguageSystem : EntitySystem
{
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
}
