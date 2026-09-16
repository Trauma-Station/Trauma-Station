// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon.Components;
using Content.Trauma.Shared.Magic.Demonologist;
using Content.Trauma.Shared.Magic.Demonologist.Components;

namespace Content.Trauma.Client.Magic.Demonologist;

/// <summary>
/// This handles status icons for demonologists and their apprentices.
/// </summary>
public sealed partial class DemonologistSystem : SharedDemonologistSystem
{

    [SubscribeLocalEvent]
    private void GetDemonologistIcon(Entity<DemonologistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    [SubscribeLocalEvent]
    private void GetApprenticeIcon(Entity<DemonologistApprenticeComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<DemonologistComponent>(ent))
            return;

        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
