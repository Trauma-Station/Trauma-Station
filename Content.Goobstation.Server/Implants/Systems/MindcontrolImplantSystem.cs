// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Implants.Components;
using Content.Goobstation.Server.Mindcontrol;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Shared.Implants;
using Content.Trauma.Common.Implants;
using Robust.Shared.Containers;

namespace Content.Goobstation.Server.Implants.Systems;

public sealed class MindcontrolImplantSystem : EntitySystem
{
    [Dependency] private readonly MindcontrolSystem _mindcontrol = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindcontrolImplantComponent, ImplanterUsedEvent>(OnImplanterUsed);
        SubscribeLocalEvent<MindcontrolImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanterUsed(Entity<MindcontrolImplantComponent> ent, ref ImplanterUsedEvent args)
    {
        if (ent.Owner != args.Implant)
            return;

        var mob = args.Target;
        var comp = EnsureComp<MindcontrolledComponent>(mob);
        comp.Master = args.User;
        _mindcontrol.Start(mob, comp);
    }

    private void OnRemoved(Entity<MindcontrolImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<MindcontrolledComponent>(args.Implanted);
    }
}
