// <Trauma>
using Content.Trauma.Common.Language.Systems;
using Content.Shared.EntityEffects;
// </Trauma>
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed partial class TraitSystem : EntitySystem
{
    // <Trauma>
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    // </Trauma>
    [Dependency] private SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    // When the player is spawned in, add all trait components selected during character creation
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Check if player's job allows to apply traits
        if (args.JobId == null ||
            !ProtoMan.Resolve<JobPrototype>(args.JobId, out var protoJob) ||
            !protoJob.ApplyTraits)
        {
            return;
        }

        ApplyTraits(args.Mob, args.Profile); // Orion-Edit
    }

    // Orion-Edit-Start
    public void ApplyTraits(EntityUid mob, HumanoidCharacterProfile profile)
    {
        foreach (var traitId in profile.TraitPreferences)
        {
            if (!ProtoMan.TryIndex<TraitPrototype>(traitId, out var traitPrototype))
            {
                Log.Error($"No trait found with ID {traitId}!");
                continue;
            }

            if (_whitelistSystem.IsWhitelistFail(traitPrototype.Whitelist, mob) ||
                _whitelistSystem.IsWhitelistPass(traitPrototype.Blacklist, mob))
                continue;

            // Begin Goobstation: Species trait support
            if (traitPrototype.IncludedSpecies.Count > 0 && !traitPrototype.IncludedSpecies.Contains(profile.Species) ||
                traitPrototype.ExcludedSpecies.Contains(profile.Species))
                continue;
            // End Goobstation: Species trait support

            // Add all components required by the prototype
            EntityManager.AddComponents(mob, traitPrototype.Components, false);

            // Add all JobSpecials required by the prototype
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(mob);
            }

            _effects.ApplyEffects(mob, traitPrototype.Effects, predicted: false); // Trauma

            // Add all JobSpecials required by the prototype
            foreach (var special in traitPrototype.Specials)
            {
                special.AfterEquip(mob);
            }

            _effects.ApplyEffects(mob, traitPrototype.Effects, predicted: false); // Trauma

            // Einstein Engines - Language begin (remove this if trait system refactor)
            // Remove/Add Languages required by the prototype
            if (traitPrototype.RemoveLanguagesSpoken is not null)
                foreach (var lang in traitPrototype.RemoveLanguagesSpoken)
                    _language.RemoveLanguage(mob, lang, true, false);

        if (traitPrototype.RemoveLanguagesUnderstood is not null)
                foreach (var lang in traitPrototype.RemoveLanguagesUnderstood)

                    _language.RemoveLanguage(mob, lang, false);

            if (traitPrototype.LanguagesSpoken is not null)
                foreach (var lang in traitPrototype.LanguagesSpoken)
                    _language.AddLanguage(mob, lang, true, false);

            if (traitPrototype.LanguagesUnderstood is not null)
                foreach (var lang in traitPrototype.LanguagesUnderstood)
                    _language.AddLanguage(mob, lang, false, true);
            // Einstein Engines - Language end

            // Add item required by the trait
            if (traitPrototype.TraitGear == null)
                continue;

            if (!TryComp(mob, out HandsComponent? handsComponent))
                continue;

            var coords = Transform(mob).Coordinates;
            var inhandEntity = Spawn(traitPrototype.TraitGear, coords);
            _sharedHandsSystem.TryPickup(mob,
                inhandEntity,
                checkActionBlocker: false,
                handsComp: handsComponent);
        }
    }
    // Orion-Edit-End
}
