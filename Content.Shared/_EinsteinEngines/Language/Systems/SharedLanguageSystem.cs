using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text;

namespace Content.Shared._EinsteinEngines.Language.Systems;

public abstract class SharedLanguageSystem : EntitySystem
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
    /// A cached instance of <see cref="PsychomanticPrototype"/>.
    /// </summary>
    public static LanguagePrototype Psychomantic { get; private set; } = default!;

    /// <summary>
    ///     A cached instance of <see cref="UniversalPrototype"/>
    /// </summary>
    public static LanguagePrototype Universal { get; private set; } = default!;

    private StringBuilder _builder = new();

    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly SharedGameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        Universal = _prototype.Index<LanguagePrototype>(UniversalPrototype);
         // Initialize the Psychomantic prototype
        Psychomantic = _prototype.Index<LanguagePrototype>(PsychomanticPrototype);

        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnSpeakerInit);

        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, DetermineEntityLanguagesEvent>(OnDetermineUniversalLanguages);
        SubscribeAllEvent<LanguagesSetMessage>(OnClientSetLanguage);

        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, MapInitEvent>((uid, _, _) => UpdateEntityLanguages(uid));
        SubscribeLocalEvent<UniversalLanguageSpeakerComponent, ComponentRemove>((uid, _, _) => UpdateEntityLanguages(uid));
    }

    public LanguagePrototype? GetLanguagePrototype(ProtoId<LanguagePrototype> id)
    {
        _prototype.TryIndex(id, out var proto);
        return proto;
    }

    /// <summary>
    ///     Obfuscate a message using the given language.
    /// </summary>
    public string ObfuscateSpeech(string message, LanguagePrototype language)
    {
        _builder.Clear();
        language.Obfuscation.Obfuscate(_builder, message, this);

        return _builder.ToString();
    }

    /// <summary>
    ///     Generates a stable pseudo-random number in the range (min, max) (inclusively) for the given seed.
    ///     One seed always corresponds to one number, however the resulting number also depends on the current round number.
    ///     This method is meant to be used in <see cref="ObfuscationMethod"/> to provide stable obfuscation.
    /// </summary>
    internal int PseudoRandomNumber(int seed, int min, int max)
    {
        // Using RobustRandom or System.Random here is a bad idea because this method can get called hundreds of times per message.
        // Each call would require us to allocate a new instance of random, which would lead to lots of unnecessary calculations.
        // Instead, we use a simple but effective algorithm derived from the C language.
        // It does not produce a truly random number, but for the purpose of obfuscating messages in an RP-based game it's more than alright.
        seed = seed ^ (_ticker.RoundId * 127);
        var random = seed * 1103515245 + 12345;
        return min + Math.Abs(random) % (max - min + 1);
    }

    #region Event handlers

    private void OnSpeakerInit(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.CurrentLanguage))
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);

        UpdateEntityLanguages((ent, ent.Comp));
    }

    private void OnDetermineUniversalLanguages(Entity<UniversalLanguageSpeakerComponent> entity, ref DetermineEntityLanguagesEvent ev)
    {
        // We only add it as a spoken language: CanUnderstand checks for ULSC itself.
        if (entity.Comp.Enabled)
            ev.SpokenLanguages.Add(PsychomanticPrototype);
    }

    private void OnClientSetLanguage(LanguagesSetMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} uid)
            return;

        var language = GetLanguagePrototype(message.CurrentLanguage);
        if (language == null || !CanSpeak(uid, language.ID))
            return;

        SetLanguage(uid, language.ID);
    }

    #endregion

    #region Public API

    public bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (language == PsychomanticPrototype || language == UniversalPrototype || TryComp<UniversalLanguageSpeakerComponent>(ent, out var uni) && uni.Enabled)
            return true;

        return Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.UnderstoodLanguages.Contains(language);
    }

    public bool CanSpeak(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        return ent.Comp.SpokenLanguages.Contains(language);
    }

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public LanguagePrototype GetLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false)
            || string.IsNullOrEmpty(ent.Comp.CurrentLanguage)
            || !_prototype.Resolve(ent.Comp.CurrentLanguage, out var proto))
            return Universal;

        return proto;
    }

    /// <summary>
    ///     Returns the list of languages this entity can speak.
    /// </summary>
    /// <remarks>This simply returns the value of <see cref="LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetSpokenLanguages(EntityUid uid)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.SpokenLanguages : [];
    }

    /// <summary>
    ///     Returns the list of languages this entity can understand.
    /// </summary
    /// <remarks>This simply returns the value of <see cref="LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid uid)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.UnderstoodLanguages : [];
    }

    public void SetLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!CanSpeak(ent, language)
            || !Resolve(ent, ref ent.Comp)
            || ent.Comp.CurrentLanguage == language)
            return;

        ent.Comp.CurrentLanguage = language;
        Dirty(ent);
    }

    /// <summary>
    ///     Adds a new language to the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void AddLanguage(
        EntityUid uid,
        ProtoId<LanguagePrototype> language,
        bool addSpoken = true,
        bool addUnderstood = true)
    {
        var knowledge = EnsureComp<LanguageKnowledgeComponent>(uid);
        var speaker = EnsureComp<LanguageSpeakerComponent>(uid);

        if (addSpoken && !knowledge.SpokenLanguages.Contains(language))
            knowledge.SpokenLanguages.Add(language);

        if (addUnderstood && !knowledge.UnderstoodLanguages.Contains(language))
            knowledge.UnderstoodLanguages.Add(language);

        Dirty(uid, knowledge);
        UpdateEntityLanguages((uid, speaker));
    }

    /// <summary>
    ///     Removes a language from the respective lists of intrinsically known languages of the given entity.
    /// </summary>
    public void RemoveLanguage(
        Entity<LanguageKnowledgeComponent?> ent,
        ProtoId<LanguagePrototype> language,
        bool removeSpoken = true,
        bool removeUnderstood = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (removeSpoken)
            ent.Comp.SpokenLanguages.Remove(language);

        if (removeUnderstood)
            ent.Comp.UnderstoodLanguages.Remove(language);

        // We don't ensure that the entity has a speaker comp. If it doesn't... Well, woe be the caller of this method.
        Dirty(ent, ent.Comp);
        UpdateEntityLanguages(ent.Owner);
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    /// <returns>True if the current language was modified, false otherwise.</returns>
    public bool EnsureValidLanguage(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.SpokenLanguages.Contains(ent.Comp.CurrentLanguage))
        {
            ent.Comp.CurrentLanguage = ent.Comp.SpokenLanguages.FirstOrDefault(UniversalPrototype);
            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Immediately refreshes the cached lists of spoken and understood languages for the given entity.
    /// </summary>
    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryComp<LanguageKnowledgeComponent>(ent, out var knowledge))
        {
            foreach (var spoken in knowledge.SpokenLanguages)
                ev.SpokenLanguages.Add(spoken);

            foreach (var understood in knowledge.UnderstoodLanguages)
                ev.UnderstoodLanguages.Add(understood);
        }

        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpokenLanguages.Clear();
        ent.Comp.UnderstoodLanguages.Clear();

        ent.Comp.SpokenLanguages.AddRange(ev.SpokenLanguages);
        ent.Comp.UnderstoodLanguages.AddRange(ev.UnderstoodLanguages);

        EnsureValidLanguage(ent);

        Dirty(ent);
    }

    #endregion
}
