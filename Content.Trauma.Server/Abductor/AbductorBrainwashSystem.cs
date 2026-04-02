// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Medical.Shared.Abductor;
using Content.Shared.Mindshield.Components;
using Content.Trauma.Shared.Mindcontrol;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorBrainwashSystem : EntitySystem
{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbductorGizmoComponent, BrainwashDoAfterEvent>(OnBrainwashDoAfterEvent);
    }

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

    private void OnBrainwashDoAfterEvent(Entity<AbductorGizmoComponent> ent, ref BrainwashDoAfterEvent args)
{
    if (args.Cancelled || args.Target is not {} target)
        return;
    if (HasComp<MindShieldComponent>(target))
        return;

    var comp = EnsureComp<MindcontrolledComponent>(target);
    comp.Master = args.User;
    comp.MindcontrolIcon = "AbductorMindControl";
    _mindcontrol.Start(target, comp);

    var timed = EnsureComp<TimedMindControlComponent>(target);
    timed.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(15);
}
}
