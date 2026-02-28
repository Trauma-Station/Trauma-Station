// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Heretic.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._Goobstation.Heretic.Components;
using Content.Shared._Goobstation.Wizard;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.StatusEffectNew;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Heretic.EntitySystems;

public sealed class EldritchInfluenceSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
        if (!ent.Comp.Spent && _heretic.TryGetHereticComponent(args.Examiner, out _, out _))
        {
            var msg = Loc.GetString(ent.Comp.HereticExamineMessage, ("tier", ent.Comp.Tier));
            args.PushMarkup(msg);
            return;
        }

        if (HasComp<SpectralComponent>(args.Examiner) || HasComp<GhostComponent>(args.Examiner) ||
            HasComp<WizardComponent>(args.Examiner) || HasComp<ApprenticeComponent>(args.Examiner) ||
            _heretic.IsHereticOrGhoul(args.Examiner))
            return;

        if (_status.HasStatusEffect(args.Examiner, ent.Comp.ExaminedRiftStatusEffect))
            return;

        if (!_mind.TryGetMind(args.Examiner, out _, out var mind))
            return;

        if (!_playerMan.TryGetSessionById(mind.UserId, out var session))
            return;

        _status.TryAddStatusEffect(args.Examiner, ent.Comp.ExaminedRiftStatusEffect, out _, ent.Comp.ExamineDelay);

        _audio.PlayGlobal(ent.Comp.ExamineSound, session);

        var baseMessage = ent.Comp.ExamineBaseMessage;
        var message = Loc.GetString(_random.Pick(ent.Comp.HeathenExamineMessages));
        var size = ent.Comp.FontSize;
        var loc = Loc.GetString(baseMessage, ("size", size), ("text", message));
        SharedChatSystem.UpdateFontSize(size, ref message, ref loc);
        _chatMan.ChatMessageToOne(ChatChannel.Server, message, loc, default, false, session.Channel, canCoalesce: false);

        var effects = _random.Pick(ent.Comp.PossibleExamineEffects);
        _effects.ApplyEffects(args.Examiner, effects);
    }

    public bool CollectInfluence(Entity<EldritchInfluenceComponent> influence, EntityUid user, EntityUid? used = null)
    {
        if (influence.Comp.Spent)
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

        _popup.PopupEntity(Loc.GetString("heretic-influence-start"), influence, user);

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
            !_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind))
            return;

        _heretic.UpdateKnowledge(args.User, 1f);

        if (TryComp(args.Used, out EldritchInfluenceDrainerComponent? drainer) &&
            drainer.TierToCategory.TryGetValue(ent.Comp.Tier, out var cat))
        {
            var current = heretic.SideKnowledgeDrafts[cat];
            heretic.SideKnowledgeDrafts[cat] = current + 1;
            if (current == 0)
                _heretic.UpdateHereticCostModifiers((mind, heretic), cat);
        }

        Spawn("EldritchInfluenceIntermediate", Transform(args.Target.Value).Coordinates);
        QueueDel(args.Target);
    }
}
