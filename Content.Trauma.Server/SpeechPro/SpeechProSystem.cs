// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Trauma.Common.Chat;
using Content.Trauma.Common.Language;
using Content.Trauma.Shared.Language.Systems;
using Content.Trauma.Shared.SpeechPro;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Server.SpeechPro;

public sealed partial class SpeechProSystem : EntitySystem
{
    private const float SecretRingtoneChance = 0.05f;
    private static readonly SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Machines/button.ogg", AudioParams.Default.WithVariation(0.125f));
    private static readonly SoundSpecifier SecretRingtone = new SoundPathSpecifier("/Audio/_Trauma/Tools/speech_pro_secret_ringtone.ogg");

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, SpeechProSpeechData> _speaking = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SpeechProComponent>(SpeechProUiKey.Key, subs =>
        {
            subs.Event<SpeechProUiMessage>(OnPhraseSelected);
        });

        SubscribeLocalEvent<SpeechProComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
        SubscribeLocalEvent<SpeechProComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ActorComponent, ChatMessageOverrideInVoiceRangeEvent>(OnChatMessageOverride);
    }

    private void OnPhraseSelected(EntityUid uid, SpeechProComponent component, SpeechProUiMessage args)
    {
        if (!_itemToggle.IsActivated(uid))
            return;

        if (args.Actor is not { Valid: true } speaker || !Exists(speaker))
            return;

        if (!_prototype.Resolve(args.Phrase, out var phrase))
            return;

        var text = Loc.GetString(phrase.Message);
        var language = _language.GetLanguage(speaker);

        _speaking[uid] = new SpeechProSpeechData(speaker, text, language);

        try
        {
            _chat.TrySendInGameICMessage(
                uid,
                text,
                InGameICChatType.Speak,
                hideChat: false,
                hideLog: false,
                ignoreActionBlocker: true,
                languageOverride: language);
        }
        finally
        {
            _speaking.Remove(uid);
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg"), uid);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{speaker:user} used Speech Pro phrase {args.Phrase:phrase} ({text}) with {uid:device}.");
    }

    private void OnTransformSpeakerName(Entity<SpeechProComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (_speaking.TryGetValue(ent.Owner, out var data))
            args.VoiceName = Loc.GetString("speech-pro-voice-name", ("user", Identity.Name(data.User, EntityManager)));
    }

    private void OnToggled(Entity<SpeechProComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated)
            return;

        var sound = _random.Prob(SecretRingtoneChance)
            ? SecretRingtone
            : OpenSound;

        _audio.PlayPvs(sound, ent.Owner);
    }

    private void OnChatMessageOverride(Entity<ActorComponent> ent, ref ChatMessageOverrideInVoiceRangeEvent args)
    {
        if (!_speaking.TryGetValue(args.Source, out var data)
            || _language.CanUnderstand(ent.Owner, data.Language.ID)
            || args.Speech is not { } speech)
            return;

        var message = _language.ObfuscateSpeech(data.Text, data.Language, data.User);
        args.Message = message;
        args.WrappedMessage = _chat.WrapPublicMessage(args.Source, args.Name, message, speech, data.Language, args.Color);
    }

    private readonly record struct SpeechProSpeechData(EntityUid User, string Text, LanguagePrototype Language);
}
