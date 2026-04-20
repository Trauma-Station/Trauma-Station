// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Blob;
using Content.Goobstation.Shared.Blob.Components;
using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Components;
using Content.Trauma.Server.Language;
using Content.Trauma.Shared.Language.Events;

namespace Content.Trauma.Server.Blob;

public sealed class TraumaBlobSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    private static readonly ProtoId<LanguagePrototype> BlobLang = "Blob";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCarrierComponent, DetermineEntityLanguagesEvent>(OnApplyLang);
        SubscribeLocalEvent<BlobSpeakComponent, DetermineEntityLanguagesEvent>(OnLanguageApply);
        SubscribeLocalEvent<BlobSpeakComponent, ComponentStartup>(OnSpokeAdd);
        SubscribeLocalEvent<BlobSpeakComponent, ComponentShutdown>(OnSpokeRemove);
    }

    private void OnApplyLang(Entity<BlobCarrierComponent> ent, ref DetermineEntityLanguagesEvent args)
    {
        if (ent.Comp.LifeStage is
           ComponentLifeStage.Removing
           or ComponentLifeStage.Stopping
           or ComponentLifeStage.Stopped)
            return;

        args.SpokenLanguages.Add(BlobLang);
        args.UnderstoodLanguages.Add(BlobLang);
    }
    private void OnLanguageApply(Entity<BlobSpeakComponent> ent, ref DetermineEntityLanguagesEvent args)
    {
        if (ent.Comp.LifeStage is
           ComponentLifeStage.Removing
           or ComponentLifeStage.Stopping
           or ComponentLifeStage.Stopped)
            return;

        args.SpokenLanguages.Clear();
        args.SpokenLanguages.Add(ent.Comp.Language);
        args.UnderstoodLanguages.Add(ent.Comp.Language);
    }

    private void OnSpokeRemove(Entity<BlobSpeakComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _language.UpdateEntityLanguages(ent.Owner);
    }

    private void OnSpokeAdd(Entity<BlobSpeakComponent> ent, ref ComponentStartup args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<LanguageSpeakerComponent>(ent, out var speaker))
            _language.EnsureValidLanguage((ent, speaker));
    }
}
