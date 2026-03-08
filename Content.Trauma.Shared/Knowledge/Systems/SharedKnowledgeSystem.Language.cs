// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage.Prototypes;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<LanguageKnowledgeComponent> _langQuery;

    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    //private static readonly HashSet<string> CursedWords = new() { "shit", "fuck", "curse", "die" };
    private HashSet<Entity<LanguageSpeakerComponent>> _hearers = new();

    private void InitializeLanguage()
    {
        _langQuery = GetEntityQuery<LanguageKnowledgeComponent>();

        SubscribeLocalEvent<LanguageKnowledgeComponent, MapInitEvent>(OnLanguageInit);
        SubscribeLocalEvent<LanguageKnowledgeComponent, KnowledgeAddedEvent>(OnLanguageAdded);
        SubscribeLocalEvent<LanguageKnowledgeComponent, KnowledgeRemovedEvent>(OnLanguageRemoved);

        SubscribeLocalEvent<LanguageSpeakerComponent, AddLanguageEvent>(OnLanguageAdd);
        SubscribeLocalEvent<LanguageSpeakerComponent, RemoveLanguageEvent>(OnLanguageRemove);
        SubscribeLocalEvent<LanguageSpeakerComponent, UpdateLanguageEvent>(OnLanguageUpdate);
        SubscribeLocalEvent<LanguageSpeakerComponent, BodyInitEvent>(OnLanguageBodyInit);

        // Experience methods
        SubscribeLocalEvent<LanguageSpeakerComponent, EntitySpokeEvent>(OnLanguageSpoke);
    }

    private void OnLanguageInit(Entity<LanguageKnowledgeComponent> ent, ref MapInitEvent args)
    {
        // to avoid copy pasting the name between each entity
        _meta.SetEntityName(ent.Owner, _language.GetLanguagePrototype(ent.Comp.LanguageId)!.Name);
    }

    private void OnLanguageAdded(Entity<LanguageKnowledgeComponent> ent, ref KnowledgeAddedEvent args)
    {
        EnsureComp<LanguageSpeakerComponent>(args.Holder);
    }

    private void OnLanguageRemoved(Entity<LanguageKnowledgeComponent> ent, ref KnowledgeRemovedEvent args)
    {
        if (args.Container.Comp.ActiveLanguage == ent.Owner)
            ChangeLanguage(args.Container, null);
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
        var ev = new DetermineEntityLanguagesEvent();
        if (GetContainer(ent.Owner) is { } brain &&
            GetKnowledgeWith<LanguageKnowledgeComponent>(brain) is { } known)
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

        // We add the intrinsically known languages first so other systems can manipulate them easily
        var lang = args.Language;
        EnsureKnowledge(brain, LanguageUnit(args.Language), 26);

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageRemove(Entity<LanguageSpeakerComponent> ent, ref RemoveLanguageEvent args)
    {
        var id = LanguageUnit(args.Language);
        if (GetContainer(ent.Owner) is not { } brain ||
            GetKnowledge(brain, id) is not { } unit)
            return;

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

    public void OnLanguageBodyInit(Entity<LanguageSpeakerComponent> ent, ref BodyInitEvent args)
    {
        if (GetContainer(ent.Owner) is not { } brain)
            return;

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

    public void OnLanguageSpoke(Entity<LanguageSpeakerComponent> ent, ref EntitySpokeEvent args)
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
        if (now < comp.LastSpoken)
            return; // on cooldown for xp and curse effects

        AddExperience(unit.AsNullable(), ent, (int) Math.Clamp((now - comp.LastSpoken).TotalSeconds, 0, 4));

        comp.LastSpoken = now + TimeSpan.FromSeconds(5);
        Dirty(unit, comp);

        /*var modifier = 0f;
        DamageSpecifier damage = default!;

        var isCurse = GetMastery(unit.Comp) >= 5 && ContainsCursedWord(args.Message);

        // need to master it to curse people
        if (isCurse)
        {
            // 0-1s, 0-20 damage
            modifier = Math.Max(((float) unit.Comp.Level - 80f) / 20f, 0f);
            damage = new DamageSpecifier();
            damage.DamageDict.Add(Blunt, 20 * modifier);
        }*/

        // curse of 220
        _hearers.Clear();
        _lookup.GetEntitiesInRange<LanguageSpeakerComponent>(_transform.GetMoverCoordinates(ent), 7f, _hearers, LookupFlags.All);
        var evheard = new AddExperienceEvent(id, 1, 10);
        foreach (var hearer in _hearers)
        {
            if (hearer.Owner == ent.Owner)
                continue; // Don't curse yourself or double dip on XP

            RaiseLocalEvent(hearer, ref evheard);

            /* too op, needs a traitor item or something + a cooldown
            if (!isCurse || !_language.CanUnderstand(hearer.Owner, args.Language))
                continue;

            _damageable.TryChangeDamage(hearer.Owner, damage, ignoreResistances: false, interruptsDoAfters: false,
                ignoreBlockers: true, targetPart: TargetBodyPart.Head, splitDamage: SplitDamageBehavior.SplitEnsureAll);
            // FIXME: this doesnt exist...
            //_status.TryAddStatusEffect(hearer, "Deafness", out _, TimeSpan.FromSeconds(modifier));

            _popup.PopupEntity(Loc.GetString("language-curse-pain"), hearer, hearer, PopupType.SmallCaution);
            */
        }
    }

    /*private bool ContainsCursedWord(string message)
    {
        // Split message into individual words to avoid catching "it" in "shit"
        // TODO: rewrite to be a regex fuck sake
        var words = message.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (CursedWords.Contains(word))
                return true;
        }
        return false;
    }*/
}
