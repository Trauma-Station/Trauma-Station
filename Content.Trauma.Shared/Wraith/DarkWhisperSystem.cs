// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Wraith;

/// <summary>
/// Lets you talk through a selected target when you speak for a certain amount of seconds
/// </summary>
public sealed class DarkWhisperSystem : EntitySystem
{
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<DarkWhisperComponent> _darkWhisperQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkWhisperComponent, DarkWhisperEvent>(OnDarkWhisper);
        SubscribeLocalEvent<DarkWhisperComponent, EntitySpokeEvent>(OnDarkWhisperSpoke);

        SubscribeAllEvent<TypingChangedEvent>(OnTypingChanged);

        _darkWhisperQuery = GetEntityQuery<DarkWhisperComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var eqe = EntityQueryEnumerator<DarkWhisperComponent>();
        while (eqe.MoveNext(out var uid, out var whisper))
        {
            if (!whisper.Active || whisper.AttachedEntity == null)
                continue;

            if (_timing.CurTime < whisper.NextUpdate)
                continue;

            _popup.PopupClient(Loc.GetString("dark-whisper-end"), uid, uid, PopupType.MediumCaution);

            whisper.Active = false;

            // Reset the visuals
            _appearance.SetData(whisper.AttachedEntity.Value, TypingIndicatorVisuals.State, TypingIndicatorState.None);

            whisper.AttachedEntity = null;
            Dirty(uid, whisper);
        }
    }

    private void OnDarkWhisper(Entity<DarkWhisperComponent> ent, ref DarkWhisperEvent args)
    {
        _popup.PopupClient(Loc.GetString("dark-whisper-start"), ent.Owner, ent.Owner, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("dark-whisper-target"), args.Target, args.Target, PopupType.MediumCaution);

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Update;
        ent.Comp.AttachedEntity = args.Target;
        ent.Comp.Active = true;
        Dirty(ent);

        args.Handled = true;
    }

    private void OnDarkWhisperSpoke(Entity<DarkWhisperComponent> ent, ref EntitySpokeEvent args)
    {
        if (!ent.Comp.Active || ent.Comp.AttachedEntity is not {} attachedEntity)
            return;

        var message = args.Message;

        _chat.TrySendInGameICMessage(
            attachedEntity,
            message,
            InGameICChatType.Speak,
            hideChat: false,
            hideLog: false,
            shell: null,
            player: null,
            nameOverride: null,
            checkRadioPrefix: true,
            ignoreActionBlocker: true);
    }

    private void OnTypingChanged(TypingChangedEvent ev, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;
        if (!Exists(uid))
            return;

        if (!_darkWhisperQuery.TryComp(uid, out var whisper) || whisper.AttachedEntity is not {} attachedEntity)
            return;

        _appearance.SetData(attachedEntity, TypingIndicatorVisuals.State, ev.State);
    }
}
