// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Item.ItemToggle;
using Content.Trauma.Common.Language;
using Content.Trauma.Shared.SpeechPro;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.SpeechPro;

public sealed partial class SpeechProSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private static readonly ProtoId<LanguagePrototype> SpeechProLanguage = "TauCetiBasic";

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SpeechProComponent>(SpeechProUiKey.Key, subs =>
        {
            subs.Event<SpeechProUiMessage>(OnPhraseSelected);
        });
    }

    private void OnPhraseSelected(EntityUid uid, SpeechProComponent component, SpeechProUiMessage args)
    {
        if (!_itemToggle.IsActivated(uid))
            return;

        if (args.Actor is not { Valid: true } speaker || !Exists(speaker))
            return;

        if (args.Phrase >= SpeechProPhrases.All.Count)
            return;

        var phrase = SpeechProPhrases.All[args.Phrase];
        var text = Loc.GetString(phrase.MessageLocId);
        var language = _prototype.Index(SpeechProLanguage);

        _chat.TrySendInGameICMessage(
            speaker,
            text,
            InGameICChatType.Speak,
            hideChat: false,
            hideLog: false,
            ignoreActionBlocker: true,
            nameOverride: "SpeechPro",
            languageOverride: language);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/quickbeep.ogg"), speaker);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(speaker)} used Speech Pro phrase '{text}' with {ToPrettyString(uid)} at {_timing.CurTime}.");
    }
}
