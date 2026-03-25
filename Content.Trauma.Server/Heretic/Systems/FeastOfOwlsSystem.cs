// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Antag;
using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Messages;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class FeastOfOwlsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly StutteringSystem _stutter = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticRitualSystem _ritual = default!;

    private readonly ProtoId<TagPrototype> _feastOfOwlsTag = "RitualFeastOfOwls";
    private readonly ProtoId<TagPrototype> _ascensionTag = "RitualAscension";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticRitualRuneComponent, FeastOfOwlsMessage>(OnMessage);
    }

    private void OnMessage(Entity<HereticRitualRuneComponent> ent, ref FeastOfOwlsMessage args)
    {
        if (!args.Accepted)
            return;

        var user = args.Actor;
        if (!_heretic.TryGetHereticComponent(user, out var heretic, out var mind))
            return;

        if (heretic.Ascended)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-already-ascended"), user, user);
            return;
        }

        if (!heretic.CanAscend)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ritual-fail-cannot-ascend"), user, user);
            return;
        }

        heretic.CanAscend = false;
        heretic.ChosenRitual = null;
        _heretic.RemoveRituals((mind, heretic), [_feastOfOwlsTag, _ascensionTag]);
        _heretic.UpdateHereticAura(user);
        Dirty(mind, heretic);

        _ritual.RitualSuccess(ent, user, false);

        _antag.SendBriefing(user,
            Loc.GetString("feast-of-owls-briefing"),
            Color.Red,
            HereticRuleSystem.BriefingSoundIntense);

        EnsureComp<FeastOfOwlsComponent>(user);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var vocalQuery = GetEntityQuery<VocalComponent>();
        var query = EntityQueryEnumerator<FeastOfOwlsComponent, StatusEffectsComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var status, out var mindContainer))
        {
            if (comp.CurrentStep >= comp.Reward)
            {
                RemCompDeferred(uid, comp);
                continue;
            }

            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.Timer;

            if (comp.CurrentStep + 1 < comp.Reward && !_stun.TryUpdateParalyzeDuration(uid, comp.ParalyzeTime))
            {
                _heretic.UpdateKnowledge(uid, comp.Reward - comp.CurrentStep, false, false, mindContainer);
                RemCompDeferred(uid, comp);
                continue;
            }

            _jitter.DoJitter(uid, comp.JitterStutterTime, true, 10f, 10f, true, status);
            _stutter.DoStutter(uid, comp.JitterStutterTime, refresh: true);

            if (vocalQuery.TryGetComponent(uid, out var vocal))
                _chat.TryEmoteWithChat(uid, vocal.ScreamId);

            _audio.PlayPvs(comp.KnowledgeGainSound, uid);

            _popup.PopupEntity(Loc.GetString("feast-of-owls-knowledge-gaim-message"), uid, uid, PopupType.LargeCaution);

            _heretic.UpdateKnowledge(uid, 1, false, false, mindContainer);

            comp.CurrentStep++;

            if (comp.CurrentStep < comp.Reward)
                continue;

            _status.TryRemoveStatusEffect(uid, "Stun", status);
            RemComp<KnockedDownComponent>(uid);
            RemCompDeferred(uid, comp);
        }
    }
}
