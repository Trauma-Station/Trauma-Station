// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Medical.Shared.Abductor;
using Content.Shared.Mindshield;
using Content.Shared.Popups;
using Content.Trauma.Shared.Mindcontrol;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorBrainwashSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MindcontrolSystem _mindcontrol = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TimedMindControlComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.ExpiresAt) continue;
            RemCompDeferred(uid, comp);
            RemCompDeferred<MindcontrolledComponent>(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnBrainwashDoAfterEvent(Entity<AbductorGizmoComponent> ent, ref BrainwashDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not {} target)
            return;

        if (_mindShield.IsShielded(target))
        {
            _popup.PopupEntity("The mindshield blocks your 7G waves!", target, args.User);
            return;
        }

        var comp = EnsureComp<MindcontrolledComponent>(target);
        comp.Master = args.User;
        comp.MindcontrolIcon = "AbductorMindControl";
        _mindcontrol.Start(target, comp);

        var timed = EnsureComp<TimedMindControlComponent>(target);
        timed.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(15);
    }
}
