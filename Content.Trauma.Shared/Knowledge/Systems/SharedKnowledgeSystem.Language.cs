using System.Linq;
using System.Net.NetworkInformation;
using Content.Shared._EinsteinEngines.Language.Components;
using Content.Shared._EinsteinEngines.Language.Events;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using YamlDotNet.Core.Tokens;

namespace Content.Trauma.Shared.Knowledge.Systems;
public abstract partial class SharedKnowledgeSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private static readonly HashSet<string> CursedWords = new() { "shit", "fuck", "curse", "die" };

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
        var ev = new DetermineEntityLanguagesEvent();
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

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

        // Updates the KnowledgeEntity that is attached to LanguageSpeaker. If this fails, LanguageSpeaker is probably attached to an item or something.
        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt2)
        {
            var knowledgeContainerComp = Comp<KnowledgeContainerComponent>(knowledgeEnt2);
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt2);

            if (knownLanguages != null)
            {
                foreach (var language in knownLanguages)
                {
                    if (ent.Comp.CurrentLanguage == language.Comp1.LanguageId)
                    {
                        knowledgeContainerComp.LanguageSkillUid = language;
                        Dirty(ent);
                        Dirty(knowledgeEnt2, knowledgeContainerComp);
                        return;
                    }
                }
            }
            // This means that there is no language skill that the user is using. (i.e. using a translator.)
            knowledgeContainerComp.LanguageSkillUid = null;
            Dirty(knowledgeEnt2, knowledgeContainerComp);
        }

        Dirty(ent);
    }

    public void OnLanguageAdded(Entity<LanguageSpeakerComponent> ent, ref AddLanguageEvent args)
    {
        // We add the intrinsically known languages first so other systems can manipulate them easily
        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

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
                    if (!TryAddKnowledgeUnit(knowledgeEnt, new KeyValuePair<EntProtoId, int>($"language-{args.Language.Id}", 26)))
                        Log.Error($"Failed to spawn language entity for language {args.Language.Id} in entity {ToPrettyString(ent)}");
                }
                // Do nothing if they already know the language
            }
            else
            {
                if (!TryAddKnowledgeUnit(knowledgeEnt, new KeyValuePair<EntProtoId, int>($"language-{args.Language.Id}", 26)))
                    Log.Error($"Failed to spawn language entity for language {args.Language.Id} in entity {ToPrettyString(ent)}");
            }
            Dirty(ent);
            UpdateEntityLanguages(ent);
        }
    }

    public void OnLanguageRemoved(Entity<LanguageSpeakerComponent> ent, ref RemoveLanguageEvent args)
    {

        if (TryGetKnowledgeEntity(ent) is { } knowledgeEnt && TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) && knowledge.KnowledgeContainer != null)
        {
            var knownLanguages = TryGetKnowledgeWithComp<LanguageKnowledgeComponent>(knowledgeEnt);

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
        if (TryGetKnowledgeEntity(ent) is not { } knowledgeEnt || !TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledgeContainer))
            return;

        EnsureContainer((knowledgeEnt, knowledgeContainer));

        //Log.Debug($"Entity {ToPrettyString(ent)} has knowledge entity {ToPrettyString(knowledgeEnt)}");
        //Log.Debug($"Entity {ToPrettyString(ent)} has {ent.Comp.Speaks.Count()} speaks and {ent.Comp.Understands.Count()} understands.");
        foreach (var spoken in ent.Comp.Speaks)
        {
            if (!TryAddKnowledgeUnit(knowledgeEnt, new KeyValuePair<EntProtoId, int>($"language-{spoken.Id}", 26)))
                Log.Error($"Failed to spawn language entity for language {spoken.Id} in entity {ToPrettyString(ent)}");
        }

        foreach (var understood in ent.Comp.Understands.Except(ent.Comp.Speaks))
        {
            if (TryAddKnowledgeUnit(knowledgeEnt, new KeyValuePair<EntProtoId, int>($"language-{understood.Id}", 26)))
                Log.Error($"Failed to spawn language entity for language {understood.Id} in entity {ToPrettyString(ent)}");
        }

        UpdateEntityLanguages(ent);
    }

    public void OnLanguageSpoke(Entity<LanguageSpeakerComponent> ent, ref EntitySpokeEvent args)
    {
        if (TryGetKnowledgeEntity(ent) is not { } knowledgeEnt)
            return;
        if (!TryComp<KnowledgeContainerComponent>(knowledgeEnt, out var knowledge) || knowledge.KnowledgeContainer == null)
            return;

        EntityUid? knownLanguage = TryGetKnowledgeUnit(knowledgeEnt, $"language-{args.Language.ID}");

        if (knownLanguage is { } knownLanguageTrue && TryComp<LanguageKnowledgeComponent>(knownLanguageTrue, out var languageKnowledgeComponent))
        {
            // Add Send Damage to all who hear.
            if (GetMastery(knownLanguageTrue) >= 5)
            {
                if (ContainsCursedWord(args.Message) && TryComp<KnowledgeComponent>(knownLanguage, out var knowledgeComponent))
                {
                    // 1. Find everyone within earshot (usually 7-10 meters for normal speech)
                    var range = 7f;
                    var entitiesNearby = _lookup.GetEntitiesInRange(ent, range);

                    var damage = new DamageSpecifier();
                    var modifier = Math.Clamp((80f - ((float) knowledgeComponent.Level) / 20f), 0, 1f);
                    damage.DamageDict.Add("Brute", 20 * modifier);

                    foreach (var hearer in entitiesNearby)
                    {
                        if (hearer == ent.Owner) continue; // Don't curse yourself

                        if (_inventory.TryGetSlotEntity(hearer, "ears", out var earItem))
                            continue;

                        if (_language.CanUnderstand(hearer, args.Language))
                        {

                            _damageable.TryChangeDamage(hearer, damage, ignoreResistances: false);
                            _status.TryAddStatusEffect(hearer, "Deafness", out _, TimeSpan.FromSeconds(modifier));

                            _popup.PopupEntity(Loc.GetString("language-curse-pain"), hearer, hearer, PopupType.SmallCaution);
                        }
                    }
                }
            }
            Log.Debug($" Time between messages: {_timing.CurTick.Value - languageKnowledgeComponent.LastSentMessage}");
            if (_timing.CurTick.Value - languageKnowledgeComponent.LastSentMessage >= 250)
            {
                var ev = new AddExperience($"language-{args.Language.ID}", Math.Clamp((int) (_timing.CurTick.Value - languageKnowledgeComponent.LastSentMessage - 250) / 100, 1, 4));
                RaiseLocalEvent(ent, ref ev);
                languageKnowledgeComponent.LastSentMessage = _timing.CurTick.Value;
                UpdateEntityLanguages(ent);
            }
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
