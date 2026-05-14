// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Speech;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Shared.Language.Components;
using Content.Trauma.Shared.Language.Events;
using Content.Trauma.Shared.Language.Systems;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private EntityQuery<LanguageKnowledgeComponent> _langQuery = default!;

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<LanguageKnowledgeComponent, MapInitEvent>(OnLanguageInit,
            after: [ typeof(InitialBodySystem) ]); // great engine
        SubscribeLocalEvent<LanguageKnowledgeComponent, KnowledgeAddedEvent>(OnLanguageAdded);
        SubscribeLocalEvent<LanguageKnowledgeComponent, KnowledgeRemovedEvent>(OnLanguageRemoved);

        SubscribeLocalEvent<LanguageSpeakerComponent, AddLanguageEvent>(OnLanguageAdd);
        SubscribeLocalEvent<LanguageSpeakerComponent, RemoveLanguageEvent>(OnLanguageRemove);
        SubscribeLocalEvent<LanguageSpeakerComponent, UpdateLanguageEvent>(OnLanguageUpdate);
        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnSpeakerMapInit,
            after: [ typeof(InitialBodySystem) ]);

        // Experience methods
        SubscribeLocalEvent<KnowledgeHolderComponent, EntitySpokeEvent>(OnLanguageSpoke);
        SubscribeLocalEvent<KnowledgeHolderComponent, ListenEvent>(OnLanguageHeard);
    }

    private void OnLanguageInit(Entity<LanguageKnowledgeComponent> ent, ref MapInitEvent args)
    {
        // to avoid copy pasting the name between each entity
        _meta.SetEntityName(ent.Owner, _language.GetLanguagePrototype(ent.Comp.LanguageId)!.Name);
    }

    private void OnLanguageAdded(Entity<LanguageKnowledgeComponent> ent, ref KnowledgeAddedEvent args)
    {
        var speaker = EnsureComp<LanguageSpeakerComponent>(args.Holder);
        UpdateEntityLanguages((args.Holder, speaker));
    }

    private void OnLanguageRemoved(Entity<LanguageKnowledgeComponent> ent, ref KnowledgeRemovedEvent args)
    {
        if (args.Container.Comp.ActiveLanguage == ent.Owner)
            ChangeLanguage(args.Container, null);

        if (TryComp<LanguageSpeakerComponent>(args.Holder, out var speaker))
            UpdateEntityLanguages((args.Holder, speaker));
    }

    /// <summary>
    /// Directly sets the current spoken language.
    /// </summary>
    public void ChangeLanguage(Entity<KnowledgeContainerComponent> ent, EntityUid? unit)
    {
        ent.Comp.ActiveLanguage = null;
        DirtyField(ent, ent.Comp, nameof(KnowledgeContainerComponent.ActiveLanguage));
    }

    /// <summary>
    /// Get the corresponding knowledge entity prototype for a given language.
    /// </summary>
    public EntProtoId LanguageUnit(ProtoId<LanguagePrototype> lang)
    {
        var id = $"Language{lang}";
        DebugTools.Assert(_proto.HasIndex<EntityPrototype>(id), $"Language {lang} has no knowledge prototype!");
        return id;
    }

    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent> ent)
    {
        if (GetContainer(ent.Owner) is not { } brain)
            return;

        var ev = new DetermineEntityLanguagesEvent();
        if (GetKnowledgeWith<LanguageKnowledgeComponent>(brain) is { } known)
        {
            foreach (var language in known)
            {
                if (language.Comp1.Speaks)
                    ev.SpokenLanguages.Add(language.Comp1.LanguageId);
                if (language.Comp1.Understands)
                    ev.UnderstoodLanguages.Add(language.Comp1.LanguageId);
            }
        }
        else
        {
            // Fallback for anything that doesn't have a knowledge container like an item.
            foreach (var spoken in ent.Comp.Speaks)
            {
                ev.SpokenLanguages.Add(spoken);
            }
            foreach (var understood in ent.Comp.Speaks)
            {
                ev.UnderstoodLanguages.Add(understood);
            }
        }

        RaiseLocalEvent(ent, ref ev);

        ent.Comp.Speaks.Clear();
        ent.Comp.Understands.Clear();

        ent.Comp.Speaks.AddRange(ev.SpokenLanguages);
        ent.Comp.Understands.AddRange(ev.UnderstoodLanguages);

        _language.EnsureValidLanguage(ent);

        SpeakerToKnowledge(ent);
    }

    private void SpeakerToKnowledge(Entity<LanguageSpeakerComponent> ent)
    {
        if (GetContainer(ent.Owner) is not { } brain ||
            GetKnowledgeWith<LanguageKnowledgeComponent>(brain) is not { } known)
            return;

        foreach (var language in known)
        {
            if (ent.Comp.CurrentLanguage == language.Comp1.LanguageId)
            {
                ChangeLanguage(brain, language);
                return;
            }
        }

        // If it gets here, this means that there is no language skill that the user is. (i.e. must use a translator.)
        ChangeLanguage(brain, null);
    }

    public void OnLanguageAdd(Entity<LanguageSpeakerComponent> ent, ref AddLanguageEvent args)
    {
        if (GetContainer(ent.Owner) is not { } brain)
            return;

        args.Handled = true;

        // We add the intrinsically known languages first so other systems can manipulate them easily
        var lang = args.Language;
        if (GetKnowledge(brain, LanguageUnit(lang)) is { } existing)
        {
            UpdateEntityLanguages(ent);
            return;
        }

        EnsureKnowledge(brain, LanguageUnit(args.Language), 26);

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageRemove(Entity<LanguageSpeakerComponent> ent, ref RemoveLanguageEvent args)
    {
        var id = LanguageUnit(args.Language);
        if (GetContainer(ent.Owner) is not { } brain ||
            GetKnowledge(brain, id) is not { } unit)
            return;

        args.Handled = true;

        var langComp = _langQuery.Comp(unit);
        if (args.RemoveSpoken && args.RemoveUnderstood)
        {
            RemoveKnowledge(brain, id);
        }
        else
        {
            langComp.Speaks = !args.RemoveSpoken;
            langComp.Understands = !args.RemoveSpoken;
            Dirty(unit, langComp);
        }

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageUpdate(Entity<LanguageSpeakerComponent> ent, ref UpdateLanguageEvent args)
    {
        UpdateEntityLanguages(ent);
    }

    public void OnSpeakerMapInit(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (GetContainer(ent.Owner) is not { } brain)
        {
            // just use mob yml languages
            return;
        }

        var allLanguages = new List<(ProtoId<LanguagePrototype>, bool)>();
        foreach (var id in ent.Comp.Speaks)
        {
            allLanguages.Add((id, true));
        }
        // don't add duplicates when you both speak and understand a language
        foreach (var id in ent.Comp.Understands)
        {
            if (!ent.Comp.Speaks.Contains(id))
                allLanguages.Add((id, false));
        }

        foreach (var (lang, speaks) in allLanguages)
        {
            if (GetKnowledge(brain, LanguageUnit(lang)) is { } existing)
                continue;

            // Add if you don't know shit.
            if (EnsureKnowledge(brain, LanguageUnit(lang), 26) is not { } unit)
            {
                Log.Error($"Failed to add language knowledge {lang} to {ToPrettyString(ent)}!");
                continue;
            }

            var comp = _langQuery.Comp(unit);
            comp.Speaks = speaks;
            comp.Understands = true;
            Dirty(unit, comp);
        }

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageSpoke(Entity<KnowledgeHolderComponent> ent, ref EntitySpokeEvent args)
    {
        if (GetContainer(ent.Owner) is not { } brain)
            return;

        var id = LanguageUnit(args.Language);
        if (GetKnowledge(brain, id) is not { } unit)
        {
            Log.Warning($"{ToPrettyString(ent)} spoke in language {args.Language} while not having knowledge of it!?");
            return;
        }

        var comp = _langQuery.Comp(unit);

        var now = _timing.CurTime;

        AddExperience(unit.AsNullable(), ent, Math.Min(args.Message.Length / 10, 8)); // The more you speak, the more you learn. Doesn't award anything for small sentences. Already does auto xp shit.

        Dirty(unit, comp);
    }

    private void OnLanguageHeard(Entity<KnowledgeHolderComponent> ent, ref ListenEvent args)
    {
        if (args.Source == ent.Owner)
            return; // Same person, no need.

        // Already Obfuscating.

        if (GetContainer(ent.Owner) is not { } brain)
            return;

        AddExperience(brain, LanguageUnit(args.Language), Math.Min(args.Message.Length / 10, 8));
    }

    public EntityUid? GetActiveLanguage(EntityUid target)
        => GetContainer(target)?.Comp.ActiveLanguage;
}
