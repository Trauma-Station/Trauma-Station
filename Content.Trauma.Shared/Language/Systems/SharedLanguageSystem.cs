// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Shared.GameTicking;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Common.Language.Systems;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.Language.Components;
using Content.Trauma.Shared.Language.Events;

namespace Content.Trauma.Shared.Language.Systems;

public abstract class SharedLanguageSystem : CommonLanguageSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedGameTicker _ticker = default!;
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    private StringBuilder _builder = new();

    public override void Initialize()
    {
        base.Initialize();

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

    public override string ObfuscateSpeech(string message, LanguagePrototype language, EntityUid messageSource)
    {
        _builder.Clear();
        var ratio = 1.0f;
        if (_knowledge.GetContainer(messageSource) is { } brain && _knowledge.GetKnowledge(brain, _knowledge.LanguageUnit(language)) is { } skill)
        {
            if (_knowledge.GetMastery(skill.Comp) > 1)
                ratio = 0.0f;
            else
                ratio = 1.0f - _knowledge.SharpCurve(skill, 0, 26);
        }
        language.Obfuscation.Obfuscate(_builder, message, this, ratio);

        return _builder.ToString();
    }

    public override int PseudoRandomNumber(int seed, int min, int max)
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

    private void OnDetermineUniversalLanguages(Entity<UniversalLanguageSpeakerComponent> entity, ref DetermineEntityLanguagesEvent ev)
    {
        // We only add it as a spoken language: CanUnderstand checks for ULSC itself.
        if (entity.Comp.Enabled)
            ev.SpokenLanguages.Add(PsychomanticPrototype);
    }

    private void OnClientSetLanguage(LanguagesSetMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        var language = GetLanguagePrototype(message.CurrentLanguage);
        if (language == null || !CanSpeak(uid, language.ID))
            return;

        SetLanguage(uid, language.ID);
    }

    #endregion

    #region Public API

    public override bool CanUnderstand(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (language == PsychomanticPrototype || language == UniversalPrototype || TryComp<UniversalLanguageSpeakerComponent>(ent, out var uni) && uni.Enabled)
            return true;

        return Resolve(ent, ref ent.Comp, logMissing: false) && ent.Comp.Understands.Contains(language);
    }

    public bool CanSpeak(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language)
    {
        if (!Resolve(ent, ref ent.Comp, logMissing: false))
            return false;

        return ent.Comp.Speaks.Contains(language);
    }

    /// <summary>
    ///     Returns the current language of the given entity, assumes Universal if it's not a language speaker.
    /// </summary>
    public override LanguagePrototype GetLanguage(Entity<LanguageSpeakerComponent?> ent)
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
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.Speaks : [];
    }

    /// <summary>
    ///     Returns the list of languages this entity can understand.
    /// </summary
    /// <remarks>This simply returns the value of <see cref="LanguageSpeakerComponent.SpokenLanguages"/>.</remarks>
    public List<ProtoId<LanguagePrototype>> GetUnderstoodLanguages(EntityUid uid)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var component) ? component.Understands : [];
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

    public override void AddLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language, bool addSpoken = true, bool addUnderstood = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new AddLanguageEvent(language, addSpoken, addUnderstood);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return;

        // normal logic for case of no knowledge
        if (addSpoken)
            ent.Comp.Speaks.Add(language);
        if (addUnderstood)
            ent.Comp.Understands.Add(language);
        Dirty(ent);
    }

    public override void RemoveLanguage(Entity<LanguageSpeakerComponent?> ent, ProtoId<LanguagePrototype> language, bool removeSpoken = true, bool removeUnderstood = true)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new RemoveLanguageEvent(language, removeSpoken, removeUnderstood);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Handled)
            return;

        // normal logic for case of no knowledge
        if (removeSpoken)
            ent.Comp.Speaks.Remove(language);
        if (removeUnderstood)
            ent.Comp.Understands.Remove(language);
        Dirty(ent);
    }

    /// <summary>
    ///   Ensures the given entity has a valid language as its current language.
    ///   If not, sets it to the first entry of its SpokenLanguages list, or universal if it's empty.
    /// </summary>
    /// <returns>True if the current language was modified, false otherwise.</returns>
    public bool EnsureValidLanguage(Entity<LanguageSpeakerComponent> ent)
    {
        if (!ent.Comp.Speaks.Contains(ent.Comp.CurrentLanguage))
        {
            ent.Comp.CurrentLanguage = ent.Comp.Speaks.FirstOrDefault(UniversalPrototype);
            Dirty(ent);
            return true;
        }

        return false;
    }

    public override void UpdateEntityLanguages(Entity<LanguageSpeakerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var ev = new UpdateLanguageEvent();
        RaiseLocalEvent(ent, ref ev);
        return;
    }

    #endregion
}

[ByRefEvent]
public record struct AddLanguageEvent(ProtoId<LanguagePrototype> Language, bool AddSpoken, bool AddUnderstood, bool Handled = false);
[ByRefEvent]
public record struct RemoveLanguageEvent(ProtoId<LanguagePrototype> Language, bool RemoveSpoken, bool RemoveUnderstood, bool Handled = false);
[ByRefEvent]
public record struct UpdateLanguageEvent();
