// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed partial class TelepathyActionSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ISharedChatManager _chatMan = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelepathyActionComponent, TelepathyActionEvent>(OnTelepathyPrompt);

        Subs.BuiEvents<TelepathyActionComponent>(TelepathyUiKey.Key, subs =>
        {
            subs.Event<TelepathyChosenMessage>(OnTelepathyChosen);
        });
    }

    private void OnTelepathyPrompt(Entity<TelepathyActionComponent> ent, ref TelepathyActionEvent args)
    {
        // for this specifically, prediction is fucked
        // but other predicted opens are fine (e.g. debug effect stick)
        // incomprehensible shitcode
        if (_net.IsClient)
            return;

        var user = args.Performer;
        var target = args.Target;
        ent.Comp.Target = target; // so it can be used later

        if (!_ui.TryOpenUi(ent.Owner, TelepathyUiKey.Key, user))
            Log.Error($"Failed to open UI for {ToPrettyString(ent)} of {ToPrettyString(user)}");

        // intentionally not handled, only start the cooldown after a message is sent
    }

    private void OnTelepathyChosen(Entity<TelepathyActionComponent> ent, ref TelepathyChosenMessage args)
    {
        var user = args.Actor;
        if (ent.Comp.Target is not {} target)
            return;

        ent.Comp.Target = null;

        var msg = args.Message.Trim();
        if (msg.Length > ent.Comp.MaxLength) // no malf
            return;

        // TODO: close it if the target leaves range

        // no prediction beyond here since client doesn't know other entities' ActorComponent
        if (_net.IsClient)
            return;

        var ident = Identity.Entity(target, EntityManager);
        if (!_actorQuery.TryComp(target, out var actor))
        {
            _popup.PopupEntity(Loc.GetString("MutationTelepathy-popup-mindless", ("target", ident)), user, user);
            return;
        }

        var channel = actor.PlayerSession.Channel;

        // start the delay now that a message is being sent
        _actions.StartUseDelay(ent.Owner);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(user)} sent a telepathic message to {ToPrettyString(target)}: {msg}");

        // TODO: handle mind magic protection with -popup-blocked
        Tell(channel, msg);
        // TODO: send message for ghosts too
    }

    private void Tell(INetChannel client, string message)
    {
        _chatMan.ChatMessageToOne(ChatChannel.Local,
            message,
            Loc.GetString("MutationTelepathy-message-wrap", ("message", FormattedMessage.EscapeText(message))),
            source: EntityUid.Invalid, // no doxxing the sender
            hideChat: false,
            client: client,
            recordReplay: true);
    }
}
