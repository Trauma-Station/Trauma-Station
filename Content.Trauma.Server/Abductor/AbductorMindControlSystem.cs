// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Medical.Shared.Abductor;
using Content.Shared.Mindshield.Components;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorMindControlSystem : EntitySystem
{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbductorGizmoComponent, AbductorGizmoMindControlDoAfterEvent>(OnGizmoMindControlDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MindcontrolledComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ExpiresAt == null)
                continue;

            if (_timing.CurTime >= comp.ExpiresAt.Value)
                RemComp<MindcontrolledComponent>(uid);
        }
    }

    private void OnGizmoMindControlDoAfter(Entity<AbductorGizmoComponent> ent, ref AbductorGizmoMindControlDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        if (HasComp<MindShieldComponent>(target))
        {
            args.Handled = true;
            return;
        }

        var comp = EnsureComp<MindcontrolledComponent>(target);
        comp.Master = args.User;
        comp.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(15);
        _mindcontrol.Start(target, comp);

        args.Handled = true;
    }
}
