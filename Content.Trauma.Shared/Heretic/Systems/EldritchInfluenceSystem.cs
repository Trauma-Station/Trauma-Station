// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Store.Components;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Heretic.Systems;

public sealed partial class EldritchInfluenceSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedDoAfterSystem _doafter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private ISharedChatManager _chat = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private EntityQuery<ActorComponent> _actorQuery = default!;

    public static EntProtoId RealityShiftIntermediate = "EldritchInfluenceIntermediate";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EldritchInfluenceComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<EldritchInfluenceComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EldritchInfluenceComponent, EldritchInfluenceDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<EldritchInfluenceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<EldritchInfluenceComponent> ent, ref ExaminedEvent args)
    {
        if (_net.IsClient)
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, args.Examiner) ||
            !_actorQuery.TryComp(args.Examiner, out var actor) ||
            _heretic.IsHereticOrGhoul(args.Examiner) ||
            _status.HasStatusEffect(args.Examiner, ent.Comp.ExaminedRiftStatusEffect))
            return;

        var session = actor.PlayerSession;
        _status.TryAddStatusEffect(args.Examiner, ent.Comp.ExaminedRiftStatusEffect, out _, ent.Comp.ExamineDelay);

        _audio.PlayGlobal(ent.Comp.ExamineSound, session);

        var baseMessage = ent.Comp.ExamineBaseMessage;
        var message = _random.Pick(ProtoMan.Index(ent.Comp.HeathenExamineMessages));
        var size = ent.Comp.FontSize;
        var loc = Loc.GetString(baseMessage, ("size", size), ("text", message));
        SharedChatSystem.UpdateFontSize(size, ref message, ref loc);
        _chat.ChatMessageToOne(ChatChannel.Server,
            message,
            loc,
            default,
            false,
            session.Channel,
            canCoalesce: false);

        var effects = _random.Pick(ent.Comp.PossibleExamineEffects);
        _effects.ApplyEffects(args.Examiner, effects, predicted: false);
    }

    public bool CollectInfluence(Entity<EldritchInfluenceComponent> influence, EntityUid user, EntityUid? used = null)
    {
        if (influence.Comp.Spent)
            return false;

        // Check in range otherwise you can collect influences from far away due to them having x-ray fixture
        if (!_interaction.InRangeUnobstructed(user, Transform(influence).Coordinates, SharedInteractionSystem.InteractionRange + 0.5f))
            return false;

        var (time, hidden) = TryComp<EldritchInfluenceDrainerComponent>(used, out var drainer)
            ? (drainer.Time, drainer.Hidden)
            : (10f, true);

        var doAfter = new EldritchInfluenceDoAfterEvent();
        var dargs = new DoAfterArgs(EntityManager, user, time, doAfter, influence, influence, used)
        {
            NeedHand = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
            MultiplyDelay = false,
            Hidden = true,
        };

        _popup.PopupPredicted(Loc.GetString("heretic-influence-start"), influence, user);

        if (!_doafter.TryStartDoAfter(dargs))
            return false;

        if (!hidden)
            EnsureComp<HereticEyeOverlayComponent>(user);

        return true;
    }

    private void OnInteract(Entity<EldritchInfluenceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !_heretic.TryGetHereticComponent(args.User, out _, out _))
            return;

        args.Handled = CollectInfluence(ent, args.User);
    }

    private void OnInteractUsing(Entity<EldritchInfluenceComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_heretic.TryGetHereticComponent(args.User, out _, out _))
            return;

        args.Handled = CollectInfluence(ent, args.User, args.Used);
    }

    private void OnDoAfter(Entity<EldritchInfluenceComponent> ent, ref EldritchInfluenceDoAfterEvent args)
    {
        var type = args.GetType();
        var da = args.DoAfter;
        // Remove eye overlay when heretic finishes gathering rift with codex. If they are gathering multiple rifts at
        // the same time - don't remove eye overlay
        if (!TryComp(args.User, out DoAfterComponent? doAfter) ||
            doAfter.DoAfters.Values.All(x =>
            {
                if (x == da || x.Completed || x.Cancelled)
                    return true;

                return _doafter.GetArgs(x).Event.GetType() != type;
            }))
            RemCompDeferred<HereticEyeOverlayComponent>(args.User);

        if (args.Cancelled || args.Target == null ||
            !_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind) ||
            // TODO: move heretic mind to the mind role
            !TryComp(mind, out StoreComponent? store) || !TryComp(mind, out MindComponent? mindComp))
            return;

        _heretic.UpdateMindKnowledge((mind, heretic, store, mindComp),
            args.User,
            HasComp<EldritchInfluenceDrainerComponent>(args.Used)
                ? SharedHereticSystem.OneKnowledgeOneSidePoint
                : SharedHereticSystem.OneKnowledgePoint);

        PredictedSpawnAtPosition(RealityShiftIntermediate, Transform(args.Target.Value).Coordinates);
        PredictedQueueDel(args.Target);
    }
}
