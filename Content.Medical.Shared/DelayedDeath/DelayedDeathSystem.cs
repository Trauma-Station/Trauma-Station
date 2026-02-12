// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Goobstation.Common.DelayedDeath;
using Content.Shared.Chat;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.DelayedDeath;

public sealed class DelayedDeathSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedDeathComponent, TargetBeforeDefibrillatorZapsEvent>(OnDefibZap);
        SubscribeLocalEvent<DelayedDeathComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DelayedDeathComponent, MobStateComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var ent, out var comp, out var mob))
        {
            if (now < comp.NextDeath || _mob.IsDead(ent, mob))
                continue;

            // go crit then dead so deathgasp can happen
            _mob.ChangeMobState(ent, MobState.Critical, mob);
            _mob.ChangeMobState(ent, MobState.Dead, mob);

            // goob code
            var ev = new DelayedDeathEvent(ent, PreventRevive: comp.PreventAllRevives);
            RaiseLocalEvent(ent, ref ev);

            if (ev.Cancelled)
            {
                RemCompDeferred(ent, comp);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(comp.DeathMessageId)) // Goobstation
                _popup.PopupEntity(Loc.GetString(comp.DeathMessageId), ent, ent, PopupType.LargeCaution);
        }
    }

    private void OnDefibZap(Entity<DelayedDeathComponent> ent, ref TargetBeforeDefibrillatorZapsEvent args)
    {
        // can't defib someone without a heart or brain pal
        args.Cancel();

        var failPopup = Loc.GetString(ent.Comp.DefibFailMessageId); // Goobstation
        _chat.TrySendInGameICMessage(args.Defib, failPopup, InGameICChatType.Speak, true);
    }

    private void OnMapInit(Entity<DelayedDeathComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDeath = _timing.CurTime + ent.Comp.DeathDelay;
    }
}
