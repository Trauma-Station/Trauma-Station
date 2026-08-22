// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Mindcontrol;
using Content.Shared.StatusIcon.Components;

namespace Content.Goobstation.Client.Mindcontrol;

public sealed partial class MindcontrolSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnGetStatusIconsEvent(Entity<MindcontrolledComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.MindcontrolIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
