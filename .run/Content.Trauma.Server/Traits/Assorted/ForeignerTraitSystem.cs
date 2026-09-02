// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Server.Language;
using Content.Trauma.Shared.Language.Components.Translators;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Storage;

namespace Content.Trauma.Server.Traits.Assorted;

public sealed partial class ForeignerTraitSystem : EntitySystem
{
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LanguageSystem _languages = default!;
    [Dependency] private StorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ForeignerTraitComponent, ComponentInit>(OnSpawn); // TraitSystem adds it after PlayerSpawnCompleteEvent so it's fine.
    }

    private void OnSpawn(Entity<ForeignerTraitComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.CantUnderstand && !entity.Comp.CantSpeak)
            Log.Warning($"Allowing entity {entity.Owner} to speak a language but not understand it leads to undefined behavior.");

        if (!TryComp<LanguageSpeakerComponent>(entity, out var speaker))
        {
            Log.Warning($"Entity {entity.Owner} does not have a LanguageSpeaker but has a ForeignerTrait!");
            return;
        }

        var spoken = speaker.Speaks;
        var alternateLanguage = spoken.Find(it => it != entity.Comp.BaseLanguage);
        if (alternateLanguage == default)
        {
            Log.Warning($"Entity {entity.Owner} does not have an alternative language to choose from (must have at least one non-GC for ForeignerTrait)!");
            return;
        }

        // Prefer a translator built specifically for this language pair,
        // over the generic foreigner's translator, since dedicated ones are usable by
        // both parties in a conversation rather than just configured one-way for this entity.
        var dedicated = GetDedicatedTranslator(alternateLanguage);
        var translatorProto = dedicated ?? entity.Comp.BaseTranslator;

        if (TryGiveTranslator(entity.Owner, translatorProto, entity.Comp.BaseLanguage, alternateLanguage, overwriteLanguages: dedicated == null, out var translator))
        {
            _languages.RemoveLanguage(entity.Owner, entity.Comp.BaseLanguage, entity.Comp.CantSpeak, entity.Comp.CantUnderstand);
        }
    }

    /// <summary>
    /// Looks for a translator prototype specifically built for the given language,
    /// Returns null if no such prototype exists.
    /// </summary>
    private EntProtoId? GetDedicatedTranslator(ProtoId<LanguagePrototype> language)
    {
        var id = $"{language.Id}Translator";
        if (!ProtoMan.HasIndex<EntityPrototype>(id))
            return null;

        return new EntProtoId(id);
    }

    /// <summary>
    /// Tries to create and give the entity a translator that translates speech between the two specified languages.
    /// </summary>
    public bool TryGiveTranslator(
        EntityUid uid,
        EntProtoId translatorPrototype,
        ProtoId<LanguagePrototype> translatorLanguage,
        ProtoId<LanguagePrototype> entityLanguage,
        bool overwriteLanguages,
        out EntityUid result)
    {
        result = EntityUid.Invalid;
        if (translatorLanguage == entityLanguage)
            return false;

        var translator = SpawnNextToOrDrop(translatorPrototype, uid);
        result = translator;

        if (!TryComp<HandheldTranslatorComponent>(translator, out var handheld))
        {
            handheld = AddComp<HandheldTranslatorComponent>(translator);
            handheld.ToggleOnInteract = true;
            handheld.SetLanguageOnInteract = true;
        }

        // Dedicated translator prototypes already ship with the correct
        // spoken/understood/required languages in YAML for both parties - don't stomp on them.
        if (overwriteLanguages)
        {
            // Allows to speak the specified language and requires entities language.
            handheld.SpokenLanguages = [translatorLanguage];
            handheld.UnderstoodLanguages = [translatorLanguage];
            handheld.RequiredLanguages = [entityLanguage];
        }

        // Try to put it in entities hand
        if (_hands.TryPickupAnyHand(uid, translator, false, false, false))
            return true;

        // Try to find a valid clothing slot on the entity and equip the translator there
        if (TryComp<ClothingComponent>(translator, out var clothing)
            && clothing.Slots != SlotFlags.NONE
            && _inventory.TryGetSlots(uid, out var slots)
            && slots.Any(it => _inventory.TryEquip(uid, translator, it.Name, true, false)))
            return true;

        // Try to put the translator into entities bag, if it has one
        if (_inventory.TryGetSlotEntity(uid, "back", out var bag)
            && TryComp<StorageComponent>(bag, out var storage))
        {
            _storage.Insert(bag.Value, translator, out _, null, storage, false, false);
        }

        return true;
    }
}
