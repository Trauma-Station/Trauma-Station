using System.Linq;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly HashSet<string> CursedWords = new() { "shit", "fuck", "curse", "die" };
    private HashSet<Entity<IComponent>> _hearers = new();

    private void InitializeLanguage()
    {
        SubscribeLocalEvent<LanguageSpeakerComponent, AddLanguageEvent>(OnLanguageAdded);
        SubscribeLocalEvent<LanguageSpeakerComponent, RemoveLanguageEvent>(OnLanguageRemoved);
        SubscribeLocalEvent<LanguageSpeakerComponent, UpdateLanguageEvent>(OnLanguageUpdated);
        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnLanguageInit);

        // Experience methods
        SubscribeLocalEvent<LanguageSpeakerComponent, EntitySpokeEvent>(OnLanguageSpoke);
    }

    public void UpdateEntityLanguages(Entity<LanguageSpeakerComponent> ent)
    {
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        var entHolder = (ent, knowledgeHolder);

        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(entHolder) is { } knowledgeEnt)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(ent);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.Speaks == true)
                        ev.SpokenLanguages.Add(language.Comp1.LanguageId);
                    if (language.Comp1.Understands == true)
                        ev.UnderstoodLanguages.Add(language.Comp1.LanguageId);
                }
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
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        var entHolder = (ent, knowledgeHolder);

        if (TryGetKnowledgeEntity(entHolder) is { } knowledgeEnt)
        {
            var knowledgeContainerComp = Comp<KnowledgeContainerComponent>(knowledgeEnt);
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(ent);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (ent.Comp.CurrentLanguage == language.Comp1.LanguageId)
                    {
                        knowledgeContainerComp.LanguageSkillUid = language;
                        Dirty(ent);
                        Dirty(knowledgeEnt, knowledgeContainerComp);
                        return;
                    }
                }
            }
            // This means that there is no language skill that the user is using. (i.e. using a translator.)
            knowledgeContainerComp.LanguageSkillUid = null;
            Dirty(knowledgeEnt, knowledgeContainerComp);
        }

        Dirty(ent);
    }

    public void OnLanguageAdded(Entity<LanguageSpeakerComponent> ent, ref AddLanguageEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        var entHolder = (ent, knowledgeHolder);
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(entHolder) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(ent);

            if (knownLanguages != null)
            {
                EntityUid? languageToAdd = null;
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.LanguageId == args.Language)
                    {
                        languageToAdd = language;
                        break;
                    }
                }

                if (languageToAdd == null)
                {
                    if (TryAddKnowledgeUnit(knowledgeEnt, ($"language-{args.Language.Id}", 26)) is not { })
                        Log.Debug($"Couldn't add {args.Language.Id} in entity {ToPrettyString(ent)}");
                }
                // Do nothing if they already know the language
            }
            else
            {
                if (TryAddKnowledgeUnit(knowledgeEnt, ($"language-{args.Language.Id}", 26)) is not { })
                    Log.Debug($"Couldn't add {args.Language.Id} in entity {ToPrettyString(ent)}");
            }
            Dirty(ent);
            UpdateEntityLanguages(ent);
        }
    }

    public void OnLanguageRemoved(Entity<LanguageSpeakerComponent> ent, ref RemoveLanguageEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        if (TryGetKnowledgeEntity((ent, knowledgeHolder)) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(ent);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (language.Comp1.LanguageId == args.Language)
                    {
                        if (args.RemoveSpoken && args.RemoveUnderstood)
                        {
                            _container.Remove(language.Owner, knowledge.KnowledgeContainer);
                            PredictedQueueDel(language.Owner);
                        }
                        else
                        {
                            language.Comp1.Speaks = !args.RemoveSpoken;
                            language.Comp1.Understands = !args.RemoveSpoken;
                            Dirty(language.Owner, language.Comp1);
                        }
                        // We don't ensure that the entity has a speaker comp. If it doesn't... Well, woe be the caller of this method.
                        UpdateEntityLanguages(ent);
                        return;
                    }
                }
            }
        }
    }

    public void OnLanguageUpdated(Entity<LanguageSpeakerComponent> ent, ref UpdateLanguageEvent args)
    {
        UpdateEntityLanguages(ent);
    }

    public void OnLanguageInit(Entity<LanguageSpeakerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        var knowledgeEnt = TryGetKnowledgeContainer((ent, knowledgeHolder));

        var allLanguages = ent.Comp.Speaks
            .Select(l => (Id: l, Speaks: true))
            .Concat(ent.Comp.Understands
                .Where(u => !ent.Comp.Speaks.Contains(u))
                .Select(u => (Id: u, Speaks: false)));

        foreach (var lang in allLanguages)
        {
            var protoId = $"language-{lang.Id}";
            var languageUnit = TryAddKnowledgeUnit(ent, (protoId, 26));

            if (languageUnit == null)
            {
                Log.Debug($"FAILED to add language {lang.Id} to {ToPrettyString(ent)}. Check Prototype ID: {protoId}");
                continue;
            }

            if (TryComp<LanguageKnowledgeComponent>(languageUnit, out var langComp))
            {
                langComp.Speaks = lang.Speaks;
                langComp.Understands = true;

                Dirty(languageUnit.Value, langComp);
            }
        }

        Dirty(ent);
        UpdateEntityLanguages(ent);
    }

    public void OnLanguageSpoke(Entity<LanguageSpeakerComponent> ent, ref EntitySpokeEvent args)
    {
        if (!TryComp<KnowledgeHolderComponent>(ent, out var knowledgeHolder))
            return;

        if (TryGetKnowledgeEntity((ent, knowledgeHolder)) is not { } knowledgeEnt)
            return;
        if (!TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) || knowledge.KnowledgeContainer == null)
            return;

        EntityUid? knownLanguage = TryGetKnowledgeUnit(knowledgeEnt, $"language-{args.Language.ID}");

        if (knownLanguage is { } knownLanguageTrue && TryComp<LanguageKnowledgeComponent>(knownLanguageTrue, out var languageKnowledgeComponent))
        {
            var modifier = 0.0f;
            bool isCurse = GetMastery(knownLanguageTrue) >= 5 && ContainsCursedWord(args.Message);
            var damage = new DamageSpecifier();

            if (TryComp<KnowledgeComponent>(knownLanguageTrue, out var knowledgeComponent))
            {
                if (isCurse)
                {
                    modifier = Math.Max(((float) knowledgeComponent.Level - 80f) / 20f, 0f);
                    damage.DamageDict.Add("Brute", 20 * modifier);
                }
                if (_timing.CurTick.Value < knowledgeComponent.LastExperienceTick + (5.0f * _timing.TickRate))
                {
                    var evSelf = new AddExperience($"language-{args.Language.ID}", (int) Math.Clamp((float) (_timing.CurTick.Value - knowledgeComponent.LastExperienceTick) / _timing.TickRate, 0, 4));
                    if (evSelf.Experience > 0)
                        RaiseLocalEvent(ent, ref evSelf);
                    UpdateEntityLanguages(ent);
                }
            }

            _hearers.Clear();
            _lookup.GetEntitiesInRange(typeof(LanguageSpeakerComponent), _transform.GetMapCoordinates(ent), 7f, _hearers, LookupFlags.All);
            var ev = new AddExperience($"language-{args.Language.ID}", 1);
            foreach (var hearer in _hearers)
            {
                RaiseLocalEvent(hearer, ref ev);

                if (!isCurse)
                    continue;

                if (hearer.Owner == ent.Owner) continue; // Don't curse yourself

                if (_language.CanUnderstand(hearer.Owner, args.Language))
                {
                    _damageable.TryChangeDamage(hearer.Owner, damage, ignoreResistances: false, interruptsDoAfters: false, ignoreBlockers: true, targetPart: TargetBodyPart.Head, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                    _status.TryAddStatusEffect(hearer, "Deafness", out _, TimeSpan.FromSeconds(modifier));

                    _popup.PopupEntity(Loc.GetString("language-curse-pain"), hearer, hearer, PopupType.SmallCaution);
                }
            }
            Dirty(knownLanguageTrue, languageKnowledgeComponent);
            return;
        }
    }
    private bool ContainsCursedWord(string message)
    {
        // Split message into individual words to avoid catching "it" in "shit"
        var words = message.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (CursedWords.Contains(word))
                return true;
        }
        return false;
    }
}
