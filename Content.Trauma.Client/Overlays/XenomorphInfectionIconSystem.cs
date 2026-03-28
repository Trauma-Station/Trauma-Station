// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Xenomorphs.Infection;
using Content.Trauma.Shared.Xenomorphs.Larva;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Overlays;

public sealed class XenomorphInfectionIconSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphInfectedComponent, GetStatusIconsEvent>(OnXenomorphInfectedGetStatusIconsEvent);
        SubscribeLocalEvent<XenomorphLarvaVictimComponent, GetStatusIconsEvent>(OnXenomorphLarvaVictimGetStatusIconsEvent);
    }

    private void OnXenomorphInfectedGetStatusIconsEvent(EntityUid uid, XenomorphInfectedComponent component, ref GetStatusIconsEvent args)
    {
        if (component.InfectedIcons.TryGetValue(component.GrowthStage, out var infectedIcon)
            && _prototype.TryIndex(infectedIcon, out var icon))
            args.StatusIcons.Add(icon);
    }

    private void OnXenomorphLarvaVictimGetStatusIconsEvent(EntityUid uid, XenomorphLarvaVictimComponent component, ref GetStatusIconsEvent args)
    {
        if (component.InfectedIcon.HasValue && _prototype.TryIndex(component.InfectedIcon.Value, out var icon))
            args.StatusIcons.Add(icon);
    }
}
