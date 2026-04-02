// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Zombies;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Server.Language;
using Content.Trauma.Shared.Language.Events;

namespace Content.Trauma.Server.Zombies;

public sealed class TraumaZombieSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!; // Goob
    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, DetermineEntityLanguagesEvent>(OnLanguageApply);
        SubscribeLocalEvent<ZombieComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        if (component.EmoteSoundsId == null
            || TerminatingOrDeleted(uid)) // Goob Change
            return;

        var comp = EnsureComp<LanguageSpeakerComponent>(uid); // Ensure they can speak language before adding language.
        var spoken = comp.Understands;
        var understood = comp.Understands;
        spoken.Clear();
        understood.Clear();
        if (!string.IsNullOrEmpty(component.ForcedLanguage)) // Should never be false, but security either way.
        {
            spoken.Add(component.ForcedLanguage);
            understood.Add(component.ForcedLanguage);
        }
        _language.EnsureValidLanguage((uid, comp));
        _language.UpdateEntityLanguages((uid, comp));
    }

    /// <summary>
    ///     This forces the languages to reset and apply only the current language for the entity based on Zombie Component.
    /// </summary>
    private void OnLanguageApply(Entity<ZombieComponent> ent, ref DetermineEntityLanguagesEvent args)
    {
        if (ent.Comp.LifeStage is ComponentLifeStage.Removing
            or ComponentLifeStage.Stopping
            or ComponentLifeStage.Stopped)
            return;

        // Clear the languages and then apply the forced language.
        args.SpokenLanguages.Clear();
        args.UnderstoodLanguages.Clear();
        args.SpokenLanguages.Add(ent.Comp.ForcedLanguage);
        args.UnderstoodLanguages.Add(ent.Comp.ForcedLanguage);
    }

    // When comp is removed, reset languages.
    private void OnShutdown(Entity<ZombieComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _language.UpdateEntityLanguages(ent.Owner); // This uses ent.Owner because UpdateEntityLanguages checks for <LanguageSpeakerComponent>.
    }
}
