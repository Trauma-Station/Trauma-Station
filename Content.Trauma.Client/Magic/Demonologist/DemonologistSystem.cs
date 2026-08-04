// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Systems;
using Content.Shared.StatusIcon.Components;
using Content.Trauma.Shared.Magic.Demonologist;

namespace Content.Trauma.Client.Magic.Demonologist;

/// <summary>
/// This handles status icons for slings and thralls
/// This also handles alerts
/// </summary>
public sealed partial class DemonologistSystem : SharedDemonologistSystem
{

    private void GetThrallIcon(Entity<ThrallComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ShadowlingComponent>(ent))
            return;

        var iconProto = ProtoMan.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconProto);
    }

    private void GetShadowlingIcon(Entity<ShadowlingComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ProtoMan.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
