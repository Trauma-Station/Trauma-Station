// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Medical.Shared.Abductor;
using Robust.Shared.IoC;

namespace Content.Trauma.Server.Abductor;

public sealed partial class AbductorMindControlSystem : EntitySystem
{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbductorGizmoComponent, AbductorGizmoMindControlDoAfterEvent>(OnGizmoMindControlDoAfter);
    }

    private void OnGizmoMindControlDoAfter(Entity<AbductorGizmoComponent> ent, ref AbductorGizmoMindControlDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        var comp = EnsureComp<MindcontrolledComponent>(target);
        comp.Master = args.User;
        _mindcontrol.Start(target, comp);

        args.Handled = true;
    }
}
