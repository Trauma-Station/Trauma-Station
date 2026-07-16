// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon.Components;

namespace Content.Goobstation.Client.Mindcontrol;

public sealed partial class MindcontrolSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Shared.Mindcontrol.MindcontrolledComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }
    private void OnGetStatusIconsEvent(Entity<Shared.Mindcontrol.MindcontrolledComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.MindcontrolIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
